from pathlib import Path
from zipfile import ZipFile
import base64
import io
import json
import os
import re
import wave

roles = [
    ('마우스오버 / 키보드 선택 이동', '반복해서 듣는 소리라 짧은 v2·v3·v4부터 비교하세요.', ['Hover/SFX_Hoverv3.wav', 'Hover/SFX_Hoverv2.wav', 'Hover/SFX_Hoverv4.wav', 'Hover/SFX_Hoverv1.wav']),
    ('일반 버튼 클릭', '현재 프로젝트의 v1을 기준으로 비교하세요.', ['GenericClick/SFX_GenericClickv1.wav', 'GenericClick/SFX_GenericClickv6.wav', 'GenericClick/SFX_GenericClickv5.wav']),
    ('유물 ↔ 스킬 탭 전환 / 필터', '빠른 이동에는 짧은 v6~v10, 강조하려면 v3을 비교하세요.', ['ChangeTab/SFX_ChangeTabv6.wav', 'ChangeTab/SFX_ChangeTabv10.wav', 'ChangeTab/SFX_ChangeTabv3.wav']),
    ('창 열기', '전용 음원은 한 개입니다. 타이틀 하위 화면에도 공용 사용 후보입니다.', ['Window/SFX_OpenWindowv1.wav']),
    ('창 닫기 / 뒤로가기', '현재 사용하는 v3과 비교하세요.', ['Window/SFX_CloseWindowv3.wav', 'Window/SFX_CloseWindowv2.wav', 'Window/SFX_CloseWindowv4.wav']),
    ('설정 옵션 변경', '해상도·언어·화면 모드 선택에 사용합니다.', ['ChangeOption/SFX_ChangeOptionv2.wav', 'ChangeOption/SFX_ChangeOptionv10.wav', 'ChangeOption/SFX_ChangeOptionv8.wav']),
    ('ON / OFF', '켜기와 끄기는 Mark / UnMark를 짝으로 비교하세요.', ['CheckBox/SFX_CheckBoxMarkv1.wav', 'CheckBox/SFX_CheckBoxUnMarkv1.wav', 'CheckBox/SFX_CheckBoxMarkv3.wav', 'CheckBox/SFX_CheckBoxUnMarkv3.wav']),
    ('드롭다운 열기 / 닫기', 'Down / Up을 한 쌍으로 비교하세요.', ['DropDown/SFX_Downv1.wav', 'DropDown/SFX_Upv1.wav', 'DropDown/SFX_Downv2.wav', 'DropDown/SFX_Upv2.wav']),
    ('슬라이더 조절', '짧은 버전부터 비교. 매 프레임 재생하지 않습니다.', ['Slider/SFX_Sliderv1.wav', 'Slider/SFX_Sliderv2.wav', 'Slider/SFX_Sliderv6.wav']),
    ('공통 장착 / 스킬 장착', '장착 성공 시 한 번. 짧은 v4를 첫 후보로 둡니다.', ['EquipItem/SFX_EquipItemEquipmentv4.wav', 'EquipItem/SFX_EquipItemEquipmentv5.wav', 'EquipItem/SFX_EquipItemEquipmentv3.wav']),
    ('공통 장착 해제', '장착음과 함께 비교하세요.', ['UnequipItem/SFX_UnequipItemv2.wav', 'UnequipItem/SFX_UnequipItemv3.wav', 'UnequipItem/SFX_UnequipItemv1.wav']),
    ('검 유물 장착 / 해제', '검 유물만 별도 소리를 줄 경우의 후보입니다.', ['EquipSword/SFX_EquipSwordv2.wav', 'EquipSword/SFX_UnequipSwordv1.wav', 'EquipSword/SFX_EquipSwordv1.wav']),
    ('보주 집기 / 놓기', 'Gem은 보주 운반 후보입니다. 실제 장착음과는 구분해 비교하세요.', ['MoveItemGem/SFX_MoveItemGemv3.wav', 'MoveItemGem/SFX_ReleaseItemGemv2.wav', 'MoveItemGem/SFX_MoveItemGemv2.wav', 'MoveItemGem/SFX_ReleaseItemGemv3.wav']),
    ('공통 드래그 집기 / 놓기', 'StorageItem의 Grab / Release를 한 쌍으로 비교하세요.', ['StorageItem/SFX_StorageItemGrabv2.wav', 'StorageItem/SFX_StorageItemReleasev3.wav', 'StorageItem/SFX_StorageItemGrabv3.wav', 'StorageItem/SFX_StorageItemReleasev2.wav']),
    ('획득 / 적용 성공 임시 후보', '전용 획득·성공 폴더는 없습니다. Buy는 구매음이므로 직접 듣고 전용 여부를 결정하세요.', ['Buy/SFX_Buyv4.wav', 'Buy/SFX_Buyv3.wav', 'Buy/SFX_Buyv6.wav']),
    ('장착 불가 / 경고 임시 후보', '전용 Error 폴더가 없습니다. ClickStone을 비교 후보로만 두었으며, 거절감이 부족하면 다른 팩을 사용하세요.', ['ClickStone/SFX_ClickStonev2.wav', 'ClickStone/SFX_ClickStonev3.wav', 'ClickStone/SFX_ClickStonev4.wav']),
]
clips = []
with ZipFile(os.environ['REVIEW_ZIP']) as archive:
    for name in sorted((n for n in archive.namelist() if n.lower().endswith('.wav')), key=lambda n: re.sub(r'\d+', lambda m: m[0].zfill(4), n)):
        data = archive.read(name)
        with wave.open(io.BytesIO(data)) as wav:
            duration = wav.getnframes() / wav.getframerate()
        clips.append(dict(name=name, duration=duration, src='data:audio/wav;base64,'+base64.b64encode(data).decode('ascii')))
assert all(name in {c['name'] for c in clips} for _, _, names in roles for name in names)
html = '''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Project Lx · UI 효과음 비교</title>
<style>
*{box-sizing:border-box}body{margin:0;background:#11151b;color:#e6e8ed;font:15px/1.6 system-ui,sans-serif}main{max-width:1080px;margin:auto;padding:32px 24px 80px}h1{font-size:28px;margin-bottom:8px}h2{font-size:20px;margin-top:32px}p{color:#aeb9c7}button,select,input{font:inherit}button{background:#263344;color:#e6efff;border:1px solid #495b72;border-radius:7px;padding:7px 12px;cursor:pointer}button:hover{background:#3d506b}button.chosen{border-color:#8fd5bd;background:#235443}section{background:#1a212c;border:1px solid #303b4b;border-radius:12px;padding:18px;margin:12px 0}section h3{font-size:17px;margin:0}section p{margin:4px 0 12px}.candidate{display:flex;gap:8px;align-items:center;flex-wrap:wrap;margin:8px 0}.candidate small{color:#aeb9c7}.pick{margin-left:auto}.toolbar{position:sticky;top:0;background:#11151bf5;border-bottom:1px solid #42506a;padding:12px 0;z-index:2;display:flex;gap:12px;align-items:center;flex-wrap:wrap}#playing{color:#8fd5bd;font-size:13px}summary{cursor:pointer;padding:10px}details{border-bottom:1px solid #303b4b}#search{width:100%;padding:10px;background:#1a212c;color:white;border:1px solid #495b72;border-radius:7px}.note{border-left:3px solid #c3a66d;padding:8px 14px}textarea{width:100%;min-height:180px;background:#131a24;color:#e6e8ed;padding:12px}audio{height:32px;max-width:300px}a{color:#a9c9ff}
</style><main>
<h1>Project Lx · UI 효과음 비교</h1>
<p>MedievalUISounds · 158개 WAV · 프로젝트에 적용하지 않은 비교 자료</p>
<p class="note">후보 순서는 파일 용도와 길이를 기준으로 정했습니다. 청음 평가를 마친 추천 순위는 아닙니다. 재생 버튼으로 비교한 뒤 마음에 드는 파일을 선택하세요. 음원은 원본 그대로이며, 아래 볼륨은 미리듣기에만 적용됩니다.</p>
<div class="toolbar"><button id="stop">정지</button><label>미리듣기 음량 <input id="volume" type="range" min="0" max="1" step="0.01" value="0.35"></label><audio id="player" controls preload="none"></audio><span id="playing">선택한 음원 없음</span></div>
<h2>동작별 비교 후보</h2><div id="roles"></div>
<h2>선택한 파일 목록</h2><p>복사해서 채팅에 보내주면 이 조합을 기준으로 연결할 수 있습니다. 페이지를 닫기 전에 목록을 복사하세요.</p><textarea id="selection" readonly></textarea><button id="copy">목록 복사</button><span id="copyStatus"></span>
<h2>전체 음원 탐색</h2><input id="search" placeholder="파일명 또는 폴더 검색"><div id="library"></div>
</main><script>
const clips=CLIP_DATA, roles=ROLE_DATA;
const byName=new Map(clips.map(c=>[c.name,c]));
const player=document.getElementById('player'), volume=document.getElementById('volume');
player.volume=Number(volume.value);volume.oninput=()=>player.volume=Number(volume.value);
document.getElementById('stop').onclick=()=>{player.pause();player.currentTime=0};
function play(name){const c=byName.get(name);player.pause();player.src=c.src;document.getElementById('playing').textContent=name;player.play().catch(()=>{document.getElementById('playing').textContent='재생 버튼을 다시 눌러주세요: '+name})}
const picks={};
function row(name,roleIndex){const c=byName.get(name),r=document.createElement('div');r.className='candidate';const b=document.createElement('button');b.textContent='▶ '+name.split('/').pop();b.onclick=()=>play(name);r.append(b);const d=document.createElement('small');d.textContent=c.duration.toFixed(3)+'초';r.append(d);if(roleIndex!==undefined){const pick=document.createElement('button');pick.className='pick';pick.textContent='이 파일 선택';pick.dataset.role=roleIndex;pick.onclick=()=>{picks[roleIndex]=name;document.querySelectorAll('[data-role="'+roleIndex+'"]').forEach(x=>{x.classList.remove('chosen');x.textContent='이 파일 선택'});pick.classList.add('chosen');pick.textContent='선택됨';document.getElementById('selection').value=roles.flatMap((r,i)=>picks[i]?[r[0]+' → '+picks[i]]:[]).join('\\n')};r.append(pick)}return r}
roles.forEach((r,i)=>{const s=document.createElement('section'),h=document.createElement('h3'),p=document.createElement('p');h.textContent=r[0];p.textContent=r[1];s.append(h,p);r[2].forEach(n=>s.append(row(n,i)));document.getElementById('roles').append(s)});
const groups={};clips.forEach(c=>(groups[c.name.split('/')[0]]??=[]).push(c));
Object.entries(groups).forEach(([name,items])=>{const d=document.createElement('details'),s=document.createElement('summary');d.dataset.names=items.map(c=>c.name).join(' ').toLowerCase();s.textContent=name+' · '+items.length+'개';d.append(s);items.forEach(c=>d.append(row(c.name)));document.getElementById('library').append(d)});
document.getElementById('search').oninput=e=>{const q=e.target.value.trim().toLowerCase();document.querySelectorAll('details').forEach(d=>{d.hidden=!d.dataset.names.includes(q);d.open=!!q&&!d.hidden})};
document.getElementById('copy').onclick=async()=>{const area=document.getElementById('selection');try{await navigator.clipboard.writeText(area.value);document.getElementById('copyStatus').textContent=' 복사했습니다'}catch{area.focus();area.select();document.getElementById('copyStatus').textContent=' Ctrl+C로 복사하세요'}};
</script></html>'''
html = html.replace('CLIP_DATA', json.dumps(clips, ensure_ascii=False)).replace('ROLE_DATA', json.dumps(roles, ensure_ascii=False))
target = Path(__file__).with_name('UI-Sound-Preview.html')
target.write_text(html, encoding='utf-8')
print(f'Created {target.resolve()} ({target.stat().st_size:,} bytes), {len(clips)} clips, {len(roles)} roles')
