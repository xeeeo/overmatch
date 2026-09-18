"""Civilian buildings and oilfield equipment, with unchanged gameplay footprints."""
from pathlib import Path
import math
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import omlib as om
from build_bulwark import mesh, prism, join_at_origin

OUT = Path(__file__).resolve().parents[2] / "game/assets/models/neutral"


def materials():
    om.reset_scene()
    return {
        "wall": om.material("Limestone", (.45, .405, .315)),
        "edge": om.material("PaleStone", (.64, .605, .49)),
        "roof": om.material("Terracotta", (.31, .12, .065)),
        "dark": om.material("Recess", (.025, .038, .038)),
        "steel": om.material("WeatheredSteel", (.16, .20, .20), metallic=.3),
        "rust": om.material("Oxide", (.30, .105, .048)),
        "glass": om.material("Glass", (.05, .18, .22), roughness=.35),
        "wood": om.material("Timber", (.22, .145, .075)),
    }


def beam(name, a, b, width, mat):
    a, b = Vector(a), Vector(b)
    rotation = tuple(math.degrees(v) for v in (b-a).to_track_quat("Z", "Y").to_euler())
    # omlib bakes location together with rotation; orient before that bake.
    return om.box(name, (width, width, (b-a).length), (a+b)/2, mat, bevel=0, rot=rotation)


def front_window(m, x, y, z, width=.4, height=.45):
    om.box("WindowRecess", (.026, width+.11, height+.10), (x, y, z), m["dark"], bevel=0)
    om.box("WindowGlass", (.033, width, height), (x+.02, y, z), m["glass"], bevel=0)
    om.box("WindowLintel", (.12, width+.15, .065), (x+.025, y, z+height/2+.045), m["edge"], bevel=.007)
    om.box("WindowSill", (.15, width+.17, .07), (x+.045, y, z-height/2-.04), m["edge"], bevel=.008)
    om.box("Mullion", (.025, .035, height), (x+.043, y, z), m["edge"], bevel=0)


def side_window(m, y, x, z):
    om.box("SideRecess", (.49, .028, .51), (x, y, z), m["dark"], bevel=0)
    om.box("SideGlass", (.40, .038, .42), (x, y, z), m["glass"], bevel=0)
    om.box("SideSill", (.57, .12, .065), (x, y, z-.27), m["edge"], bevel=.006)
    om.box("SideMullion", (.032, .045, .42), (x, y, z), m["edge"], bevel=0)


def finish(name):
    obj = join_at_origin(name, [o for o in bpy.context.scene.objects if o.type == "MESH"], (0, 0, 0))
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    OUT.mkdir(parents=True, exist_ok=True)
    om.export_glb(str(OUT / (name + ".glb")))


def civilian_house():
    m = materials()
    om.box("Foundation", (2.45, 2.24, .19), (0, 0, .095), m["steel"], bevel=.025)
    om.box("Walls", (2.32, 2.10, 1.21), (0, 0, .795), m["wall"], bevel=.018)
    for x in (-1.14, 1.14):
        for y in (-1.02, 1.02):
            om.box("CornerStone", (.12, .12, 1.24), (x, y, .8), m["edge"], bevel=.01)
    # Genuine pitched roof, with a ridge and broad tile courses visible from above.
    vertices = [(x, y, z) for x in (-1.27, 1.23) for y,z in ((-1.18,1.4),(0,2.07),(1.18,1.4))]
    mesh("GabledRoof", vertices, [(0,3,4,1),(1,4,5,2),(0,1,2),(3,5,4),(0,2,5,3)], m["roof"])
    om.box("Ridge", (2.56, .13, .10), (-.02, 0, 2.065), m["roof"], bevel=.025)
    for side in (-1,1):
        om.box("Fascia", (2.54, .06, .13), (-.02,side*1.17,1.4), m["wood"], bevel=0)
        for y in (.22,.46,.70,.94):
            beam("TileCourse", (-1.26,side*y,2.085-y*.568),(1.22,side*y,2.085-y*.568),.025,m["rust"])
    om.box("DoorFrame", (.045,.59,1.04),(1.17,-.55,.69),m["edge"],bevel=.006)
    om.box("Door",(.055,.43,.90),(1.196,-.55,.65),m["wood"],bevel=0)
    for z in (.40,.84):
        om.box("DoorPanel",(.013,.31,.27),(1.23,-.55,z),m["dark"],bevel=0)
    om.box("Step",(.21,.72,.12),(1.13,-.55,.1),m["edge"],bevel=.014)
    front_window(m,1.168,.53,.85)
    for x in (-.62,.58):
        side_window(m,-1.061,x,.86)
        side_window(m,1.061,x,.86)
    om.box("Chimney",(.30,.33,.74),(-.72,.62,1.97),m["wall"],bevel=.012)
    om.box("ChimneyCap",(.37,.40,.08),(-.72,.62,2.35),m["edge"],bevel=.013)
    om.box("Flue",(.19,.22,.016),(-.72,.62,2.396),m["dark"],bevel=0)
    finish("civilian_house")


def civilian_block():
    m = materials()
    om.box("Foundation",(3.45,3.25,.18),(0,0,.09),m["steel"],bevel=.02)
    om.box("Building",(3.30,3.10,2.92),(0,0,1.59),m["wall"],bevel=.026)
    for z in (.24,1.24,2.13,3.02):
        om.box("StoneCourse",(3.39,3.19,.075),(0,0,z),m["edge"],bevel=.007)
    # Flat roof enclosed by a parapet, with visible stairwell and cooling plant.
    om.box("Roof",(3.33,3.13,.10),(0,0,3.10),m["steel"],bevel=.01)
    for side in (-1,1):
        om.box("ParapetX",(.09,3.2,.19),(side*1.69,0,3.21),m["edge"],bevel=.01)
        om.box("ParapetY",(3.45,.09,.19),(0,side*1.59,3.21),m["edge"],bevel=.01)
    for z in (.81,1.72,2.58):
        for offset in (-1.04,0,1.04):
            if offset != 0 or z > 1:
                front_window(m,1.656,offset,z,.44,.51)
            side_window(m,1.557,offset,z)
            side_window(m,-1.557,offset,z)
    om.box("EntryFrame",(.04,.84,1.05),(1.672,0,.60),m["edge"],bevel=.012)
    om.box("Entry",(.055,.65,.97),(1.694,0,.56),m["dark"],bevel=0)
    om.box("DoorRail",(.02,.032,.9),(1.727,0,.56),m["steel"],bevel=0)
    for x in (-.74,.6):
        om.box("RoofVent",(.58,.55,.16),(x,.78,3.19),m["edge"],bevel=.02)
        for dx in (-.16,0,.16):
            om.box("VentSlot",(.052,.39,.015),(x+dx,.78,3.279),m["dark"],bevel=0)
    om.box("RoofHatch",(.68,.76,.075),(-.68,-.67,3.18),m["dark"],bevel=.01)
    om.box("RoofHatchLid",(.61,.68,.065),(-.68,-.67,3.24),m["steel"],bevel=.015)
    finish("civilian_block")


def oil_derrick():
    m = materials()
    om.box("ConcretePad",(2.6,2.6,.16),(0,0,.08),m["wall"],bevel=.035)
    om.box("MachinePlinth",(1.80,.90,.16),(.10,-.28,.24),m["steel"],bevel=.02)
    # A-frame, counterweight and walking beam make the pump recognisable at RTS scale.
    for side in (-1,1):
        beam("AFrameFront",(.66,side*.30-.28,.32),(.04,side*.30-.28,2.03),.10,m["steel"])
        beam("AFrameRear",(-.65,side*.30-.28,.32),(.04,side*.30-.28,2.03),.10,m["steel"])
        beam("CrossBrace",(-.46,side*.30-.28,.83),(.44,side*.30-.28,.83),.07,m["rust"])
    om.cylinder("Bearing",.16,.79,(.04,-.28,2.04),m["steel"],verts=10,rot=(90,0,0),bevel=0)
    beam("WalkingBeam",(-.85,-.28,2.22),(1.01,-.28,2.40),.14,m["rust"])
    prism("HorseHead",[(.82,-.49),(1.13,-.49),(1.22,-.36),(1.22,-.20),(1.13,-.07),(.82,-.07)],1.90,2.64,m["rust"],inset=.04)
    beam("PolishedRod",(1.15,-.28,.32),(1.15,-.28,2.12),.035,m["steel"])
    om.cylinder("Gearbox",.29,.42,(-.58,-.28,.71),m["dark"],verts=12,rot=(90,0,0),bevel=.014)
    for side in (-1,1):
        beam("Pitman",(-.70,-.28+side*.24,.74),(-.70,-.28+side*.24,2.21),.05,m["steel"])
        om.box("Counterweight",(.49,.12,.27),(-.76,-.28+side*.27,.59),m["rust"],bevel=.025)
    om.cylinder("Reservoir",.45,1.11,(-.78,.79,.75),m["edge"],verts=16,bevel=.025)
    for z in (.31,1.19):
        om.cylinder("TankRim",.467,.052,(-.78,.79,z),m["steel"],verts=16,bevel=0)
    om.cylinder("TankLid",.25,.07,(-.78,.79,1.35),m["steel"],verts=10,bevel=.012)
    beam("FlowPipe",(-.78,.79,.38),(-.78,-.90,.38),.09,m["rust"])
    beam("ExportPipe",(-.78,-.9,.38),(.96,-.9,.38),.09,m["rust"])
    om.box("ControlBox",(.34,.34,.48),(.66,.83,.47),m["steel"],bevel=.02)
    om.box("ControlFace",(.016,.25,.26),(.84,.83,.5),m["dark"],bevel=0)
    for x in (-1.1,-.80,-.5,-.2,.1,.4,.7,1.0):
        om.box("PadMarker",(.12,.11,.012),(x,-1.2,.17),m["edge"],bevel=0)
    finish("oil_derrick")


def hole():
    m = materials()
    # Open annular trench lip, with the cavity kept visibly dark and low.
    vertices=[]
    for radius,z in ((.54,.04),(.48,.17),(.33,.13),(.30,.025)):
        for i in range(12):
            a=i*math.tau/12
            r=radius*(1+.05*math.sin(i*7))
            vertices.append((r*math.cos(a),r*math.sin(a),z))
    faces=[]
    for row in range(3):
        for i in range(12):
            faces.append((row*12+i,row*12+(i+1)%12,(row+1)*12+(i+1)%12,(row+1)*12+i))
    mesh("ExcavatedLip",vertices,faces,m["wall"])
    om.cylinder("PitShadow",.31,.012,(0,0,.023),m["dark"],verts=12,bevel=0)
    for x in (-.18,.01,.20):
        om.box("Lining",(.09,.20,.12),(x,-.31,.078),m["wood"],bevel=.008,rot=(0,0,7))
    om.box("Plank",(.78,.105,.045),(.04,.29,.192),m["wood"],bevel=.008,rot=(0,0,18))
    finish("hole")


BUILDERS = {"civilian_house": civilian_house, "civilian_block": civilian_block, "oil_derrick": oil_derrick, "hole": hole}

if __name__ == "__main__":
    args = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else list(BUILDERS)
    for name in args:
        BUILDERS[name]()
