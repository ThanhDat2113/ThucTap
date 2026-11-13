from pathlib import Path
import re, uuid

ROOT=Path(r"C:\Học\ThucTap\Assets\Phu\Sprite asset\Velvet Animations")
PPU="375"

def sprite_ref(p):
    meta=Path(str(p)+".meta")
    text=meta.read_text(encoding="utf-8")
    text=re.sub(r"spritePixelsToUnits:\s*[0-9.]+",f"spritePixelsToUnits: {PPU}",text)
    meta.write_text(text,encoding="utf-8",newline="\n")
    guid=re.search(r"^guid:\s*([0-9a-f]+)",text,re.M).group(1)
    ids=re.findall(r"internalID:\s*(-?\d+)",text)
    file_id=next((x for x in ids if x!="0"),None)
    if not file_id: raise RuntimeError(f"No sprite internalID in {meta}")
    return file_id,guid

def clip_yaml(name,refs,fps,loop):
    curve=[]; mapping=[]
    for i,(fid,guid) in enumerate(refs):
        t=i/fps
        curve += [f"    - time: {t:.8g}",f"      value: {{fileID: {fid}, guid: {guid}, type: 3}}"]
        mapping.append(f"    - {{fileID: {fid}, guid: {guid}, type: 3}}")
    stop=len(refs)/fps
    return "\n".join([
"%YAML 1.1","%TAG !u! tag:unity3d.com,2011:","--- !u!74 &7400000","AnimationClip:",
"  m_ObjectHideFlags: 0","  m_CorrespondingSourceObject: {fileID: 0}","  m_PrefabInstance: {fileID: 0}","  m_PrefabAsset: {fileID: 0}",f"  m_Name: {name}","  serializedVersion: 7","  m_Legacy: 0","  m_Compressed: 0","  m_UseHighQualityCurve: 1","  m_RotationCurves: []","  m_CompressedRotationCurves: []","  m_EulerCurves: []","  m_PositionCurves: []","  m_ScaleCurves: []","  m_FloatCurves: []","  m_PPtrCurves:","  - serializedVersion: 2","    curve:",*curve,
"    attribute: m_Sprite","    path: ","    classID: 212","    script: {fileID: 0}","    flags: 2","  m_SampleRate: 60","  m_WrapMode: 0","  m_Bounds:","    m_Center: {x: 0, y: 0, z: 0}","    m_Extent: {x: 0, y: 0, z: 0}","  m_ClipBindingConstant:","    genericBindings:","    - serializedVersion: 2","      path: 0","      attribute: 0","      script: {fileID: 0}","      typeID: 212","      customType: 23","      isPPtrCurve: 1","      isIntCurve: 0","      isSerializeReferenceCurve: 0","    pptrCurveMapping:",*mapping,
"  m_AnimationClipSettings:","    serializedVersion: 2","    m_AdditiveReferencePoseClip: {fileID: 0}","    m_AdditiveReferencePoseTime: 0","    m_StartTime: 0",f"    m_StopTime: {stop:.8g}","    m_OrientationOffsetY: 0","    m_Level: 0","    m_CycleOffset: 0","    m_HasAdditiveReferencePose: 0",f"    m_LoopTime: {1 if loop else 0}","    m_LoopBlend: 0","    m_LoopBlendOrientation: 0","    m_LoopBlendPositionY: 0","    m_LoopBlendPositionXZ: 0","    m_KeepOriginalOrientation: 0","    m_KeepOriginalPositionY: 1","    m_KeepOriginalPositionXZ: 0","    m_HeightFromFeet: 0","    m_Mirror: 0","  m_EditorCurves: []","  m_EulerEditorCurves: []","  m_HasGenericRootTransform: 0","  m_HasMotionFloatCurves: 0","  m_Events: []",""])

clips=ROOT/"Clips"; clips.mkdir(parents=True,exist_ok=True)
folder_meta=Path(str(clips)+".meta")
if not folder_meta.exists(): folder_meta.write_text(f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n",encoding="utf-8")

sets=[("Velvet_Dance",ROOT/"Dance"/"Frames",48,True),("Velvet_Idle",ROOT/"Idle"/"Frames",10,True),("Velvet_Miss",ROOT/"Miss"/"Frames",12,False)]
for name,folder,fps,loop in sets:
    files=sorted(folder.glob("*.png")); refs=[sprite_ref(p) for p in files]
    out=clips/f"{name}.anim"; out.write_text(clip_yaml(name,refs,fps,loop),encoding="utf-8",newline="\n")
    meta=Path(str(out)+".meta")
    if not meta.exists(): meta.write_text(f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 7400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n",encoding="utf-8")
    print(name,len(files),fps,"loop" if loop else "once")
