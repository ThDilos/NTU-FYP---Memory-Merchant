// Original procedural score and effects; no samples or external services required.
// Run: node Tools/Audio/generate-game-audio.cjs
const fs = require('fs');
const path = require('path');
const out = path.resolve(__dirname, '../../Assets/Marcus/Audio/Resources/MemoryMerchantAudio');
fs.mkdirSync(out, { recursive: true });
const rate = 44100, tau = Math.PI * 2;
let seed = 8713;
const noise = () => { seed = (1664525 * seed + 1013904223) >>> 0; return seed / 2147483648 - 1; };
const hz = n => 440 * 2 ** ((n - 69) / 12);
function wav(name, channels, peak) {
  const length = channels[0].length, count = channels.length;
  const b = Buffer.alloc(44 + length * count * 2);
  b.write('RIFF'); b.writeUInt32LE(b.length - 8, 4); b.write('WAVEfmt ', 8);
  b.writeUInt32LE(16,16); b.writeUInt16LE(1,20); b.writeUInt16LE(count,22);
  b.writeUInt32LE(rate,24); b.writeUInt32LE(rate*count*2,28); b.writeUInt16LE(count*2,32);
  b.writeUInt16LE(16,34); b.write('data',36); b.writeUInt32LE(length*count*2,40);
  let max = 0;
  for (const c of channels) for (const x of c) max = Math.max(max, Math.abs(x));
  const scale = peak / Math.max(max, 0.00001);
  let energy = 0;
  for(let i=0;i<length;i++) for(let c=0;c<count;c++) {
    const x = channels[c][i]*scale; energy += x*x;
    b.writeInt16LE(Math.round(Math.max(-1,Math.min(1,x))*32767),44+(i*count+c)*2);
  }
  fs.writeFileSync(path.join(out,name+'.wav'),b);
  return {name, seconds:length/rate, channels:count, peak, rms:Math.sqrt(energy/(length*count))};
}
function music(name,bpm,transpose,memory=false,puzzle=false) {
  const beat=60/bpm, length=Math.round(64*beat*rate);
  const l=new Float32Array(length),r=new Float32Array(length);
  function note(midi,start,beats,gain,kind='piano',pan=0) {
    const duration=(kind==='pad'?beats*beat+1.5:kind==='bell'?2.6:2.2);
    const f=hz(midi+transpose), at=Math.round(start*beat*rate);
    for(let j=0;j<duration*rate;j++) {
      const t=j/rate, gate=Math.min(1,t/(kind==='pad'?.42:.008));
      let v;
      if(kind==='pad') {
        const env=gate*Math.min(1,Math.max(0,(duration-t)/1.5));
        v=(Math.sin(tau*f*t)+.2*Math.sin(tau*f*2*t))*env*.35;
      } else {
        const decay=kind==='bell'?1.2:.65;
        v=(Math.sin(tau*f*t)*Math.exp(-t/decay)+.27*Math.sin(tau*f*2*t)*Math.exp(-t/.25)+.10*Math.sin(tau*f*(kind==='bell'?3.002:3)*t)*Math.exp(-t/.17))*gate;
        v*=Math.min(1,(duration-t)/.08);
      }
      const i=(at+j)%length;
      l[i]+=v*gain*Math.sqrt((1-pan)*.5);r[i]+=v*gain*Math.sqrt((1+pan)*.5);
    }
  }
  // Cmaj7 / Gadd9 / Am7 / Fmaj7 / C/E / Dm7 / F / Gsus-G.
  const chords=[[48,55,59,64],[43,55,62,69],[45,57,60,64],[41,53,57,64],
    [40,55,60,64],[38,53,57,60],[41,53,57,60],[43,55,60,62]];
  const melody=[[[0,76],[1.5,79],[2.5,74]],[[.5,74],[2,71]],
    [[0,72],[1,76],[2.5,79]],[[0,77],[2,76],[3,72]],
    [[.5,76],[2,74],[3,72]],[[0,69],[1.5,72],[3,74]],
    [[0,72],[2,69]],[[0,67],[2,71],[3,74]]];
  for(let bar=0;bar<16;bar++) {
    const c=chords[bar%8],start=bar*4;
    c.forEach((n,i)=>note(n,start,3.5,.025,'pad',(i-1.5)*.35));
    note(c[0],start,2,.12,'piano',-.25);
    for(let step=0;step<8;step++) {
      if(memory&&step%2)continue;
      const index=[1,2,3,2,1,2,3,2][step];
      note(c[index]+12,start+step*.5,.5,memory?.035:.055,'piano',step%2?.18:-.18);
    }
    for(const [b,n] of melody[bar%8]) {
      const variation=bar>=8&&bar%4===2?12:0;
      note(n+variation,start+b,1,.10,'bell',.12);
    }
    if(puzzle)for(const b of [1,3])note(c[1]+12,start+b,.5,.045,'piano',-.1);
  }
  // Circular reverb preserves note tails across the exact musical loop boundary.
  const dryL=l.slice(),dryR=r.slice();
  for(const [delay,gain] of [[.19,.13],[.37,.09],[.61,.065],[.89,.035]]) {
    const d=Math.round(delay*rate);
    for(let i=0;i<length;i++) {const j=(i+d)%length;l[j]+=dryR[i]*gain;r[j]+=dryL[i]*gain;}
  }
  return wav(name,[l,r],.62);
}
function effect(name,duration,render,peak=.65) {
  const a=new Float32Array(Math.ceil(duration*rate));
  for(let i=0;i<a.length;i++) {
    const t=i/rate;
    a[i]=render(t)*Math.min(1,t/.0015)*Math.min(1,(duration-t)/.008);
  }
  return wav(name,[a],peak);
}
const report=[];
report.push(music('music_shop',72,0));
report.push(music('music_memory',60,-3,true));
report.push(music('music_puzzle',88,5,false,true));
report.push(effect('button',.095,t=>Math.exp(-t/ .022)*(.7*Math.sin(tau*760*t)+.2*Math.sin(tau*1140*t)+noise()*.12),.40));
report.push(effect('dialogue',.042,t=>Math.exp(-t/.009)*(.8*Math.sin(tau*510*t)+.12*Math.sin(tau*1020*t)),.26));
report.push(effect('purchase',.65,t=>[0,.075,.15].reduce((v,at,i)=>{const x=t-at;return x<0?v:v+Math.exp(-x/.12)*(Math.sin(tau*[1318.51,1648.14,2093][i]*x)+.25*Math.sin(tau*[2720,3400,4310][i]*x));},0),.57));
for(let i=0;i<2;i++) {
  let low=0;
  report.push(effect('footstep_'+i,.17,t=>{low=.78*low+.22*noise();return Math.exp(-t/.035)*(.65*low+.4*Math.sin(tau*(96+i*13)*t)*Math.exp(-t/.025))+.07*low*Math.exp(-(((t-.045)/.02)**2));},.46));
}
report.push(effect('interact',.21,t=>Math.exp(-t/.038)*(.65*Math.sin(tau*330*t)+.3*Math.sin(tau*660*t))+(t<.04?0:.35*Math.exp(-(t-.04)/.045)*Math.sin(tau*880*(t-.04))),.43));
report.push(effect('collect',.48,t=>[0,.08,.16].reduce((v,at,i)=>{const x=t-at;return x<0?v:v+.5*Math.exp(-x/.085)*Math.sin(tau*[660,880,1108.7][i]*x);},0),.48));
report.push(effect('place',.12,t=>Math.exp(-t/.022)*(.5*Math.sin(tau*440*t)+.3*noise()),.4));
fs.writeFileSync(path.join(__dirname,'audio-manifest.json'),JSON.stringify({sampleRate:rate,provenance:'Original procedural composition and synthesis. No third-party samples.',assets:report},null,2)+'\n');
console.log(report.map(x=>`${x.name}: ${x.seconds.toFixed(3)}s, ${x.channels}ch, RMS ${x.rms.toFixed(4)}`).join('\n'));
