"""Original Directorate art: reinforced industrial architecture and heavy angular machinery.

All coordinates retain the existing metre-scale footprint, +X forward and +Z up.
The animated Turret names and their pivots are part of the game contract.
"""
import math
import os
import bpy
import bmesh
from mathutils import Vector
import omlib as om

OUT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', 'game', 'assets', 'models', 'directorate'))
P = {}


def begin(infantry=False):
    om.reset_scene()
    global P
    P = {
        'armour': om.material('DirectorateArmour', (.235, .225, .178), .83),
        'edge': om.material('ArmourEdge', (.34, .325, .265), .78),
        'dark': om.material('Mechanics', (.025, .031, .028), .94),
        'metal': om.material('IndustrialSteel', (.17, .19, .18), .55, .4),
        'concrete': om.material('Skin' if infantry else 'ReinforcedConcrete', (.53, .34, .23) if infantry else (.39, .375, .33), .9),
        'glass': om.material('Optics', (.028, .21, .25), .28, .12),
        'mark': om.material('SafetyMarking', (.66, .46, .105), .76),
        'team': om.team(),
    }


def box(name, size, loc, mat='armour', bevel=.012, rot=(0, 0, 0)):
    return om.box(name, size, loc, P[mat], bevel=bevel, rot=rot)


def cyl(name, r, depth, loc, mat='metal', verts=12, rot=(0, 0, 0), bevel=0):
    return om.cylinder(name, r, depth, loc, P[mat], verts=verts, rot=rot, bevel=bevel)


def raw(name, verts, faces, mat='armour'):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    bm = bmesh.new(); bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data); bm.free()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(P[mat])
    return obj


def prism(name, points, low, high, mat='armour', inset=.08):
    cx = sum(p[0] for p in points) / len(points)
    cy = sum(p[1] for p in points) / len(points)
    n = len(points)
    vs = [(x, y, low) for x, y in points]
    vs += [(cx+(x-cx)*(1-inset), cy+(y-cy)*(1-inset), high) for x, y in points]
    fs = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    fs += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    return raw(name, vs, fs, mat)


def octagon(name, sx, sy, low, high, center=(0, 0), mat='armour', inset=.10):
    x, y = center; a=sx/2; b=sy/2; c=min(a,b)*.24
    return prism(name, [(x+a-c,y-b),(x+a,y-b+c),(x+a,y+b-c),(x+a-c,y+b),
                        (x-a+c,y+b),(x-a,y+b-c),(x-a,y-b+c),(x-a+c,y-b)], low, high, mat, inset)


def beam(name, a, b, width, mat='metal'):
    a=Vector(a); b=Vector(b)
    rotation=(b-a).to_track_quat('Z','Y').to_euler()
    return box(name, (width,width,(b-a).length), (a+b)/2, mat, 0,
               tuple(math.degrees(r) for r in rotation))


def bake_join(name, parts=None, origin=(0,0,0)):
    parts=parts if parts is not None else [o for o in bpy.context.scene.objects if o.type=='MESH']
    for obj in parts:
        bpy.context.view_layer.objects.active=obj
        for mod in list(obj.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
    result=om.join(name, parts, origin=origin)
    # A rotated boom/horn must not leave a rotated top-level or Turret rest node.
    bpy.context.view_layer.objects.active=result
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return result


def end(name, budget=6000):
    bpy.context.view_layer.update()
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    tris=sum(len(p.vertices)-2 for o in objects for p in o.data.polygons)
    used={m.name for o in objects for m in o.data.materials}
    assert tris<=budget, (name, tris, budget)
    assert len(used)<=8, (name, used)
    assert 'TeamColour' in used
    pivots={'vanguard':(-.15,0,.85),'colossus':(-.15,0,.85),
            'typhoon':(-.5,0,.85),'hailstorm':(-.1,0,.85),'flak_tower':(0,0,1.85)}
    if name in pivots:
        turret=bpy.data.objects['Turret']
        assert turret.parent and turret.parent.name=='Hull', name
        assert (turret.location-Vector(pivots[name])).length<1e-5, (name,'turret pivot')
        assert all(abs(v)<1e-5 for v in turret.rotation_euler), (name,'turret rest rotation')
    os.makedirs(OUT, exist_ok=True)
    om.export_glb(os.path.join(OUT,name+'.glb'))
    print(f'[directorate] {name}: {tris} triangles, {len(used)} materials')


def vents(center, size, axis='x', count=6, mat='metal'):
    x,y,z=center; sx,sy=size
    box('VentRecess',(sx,sy,.025),(x,y,z),'dark',0)
    for i in range(count):
        u=(i+.5)/count-.5
        dims=(sx/count*.46,sy*.87,.017) if axis=='x' else (sx*.87,sy/count*.46,.017)
        pos=(x+u*sx,y,z+.017) if axis=='x' else (x,y+u*sy,z+.017)
        box('CoolingLouvre',dims,pos,mat,0)


def belt(length, y, height=.43, width=.35):
    # Hollow, end-wrapped track, with actual road wheels visible between belt runs.
    r=height/2; z=.08+r; c=length/2-r
    contour=[]
    for x, angles in ((c,range(-90,91,30)),(-c,range(90,271,30))):
        contour += [(x+r*math.cos(math.radians(a)),z+r*math.sin(math.radians(a))) for a in angles]
    inner=[(x*.96,z+(zz-z)*.61) for x,zz in contour]; n=len(contour)
    vs=[(x,yy,zz) for yy in (y-width/2,y+width/2) for ring in (contour,inner) for x,zz in ring]
    fs=[]
    for i in range(n):
        j=(i+1)%n
        fs += [(i,j,2*n+j,2*n+i),(n+i,3*n+i,3*n+j,n+j),(i,n+i,n+j,j),(2*n+i,2*n+j,3*n+j,3*n+i)]
    raw('ContinuousTrack',vs,fs,'dark')
    sign=1 if y>0 else -1
    for i in range(5):
        x=-c+i*2*c/4
        cyl('RoadWheel',r*.77,width*.91,(x,y,z),'metal',10,(90,0,0))
        cyl('WheelHub',r*.35,.023,(x,y+sign*(width/2+.01),z),'edge',8,(90,0,0))
    for i in range(10):
        x=-c+(i+.5)*2*c/10
        box('TrackShoe',(.10,width+.015,.019),(x,y,.09),'metal',0)
        box('TrackShoe',(.10,width+.015,.019),(x,y,.07+height),'dark',0)


def tracked_hull(length, width, track_y, top=.77, heavy=False):
    octagon('LowerChassis',length,width*.93,.27,.55,mat='dark',inset=.03)
    octagon('ArmouredHull',length,width,.42,top,mat='armour',inset=.08)
    for side in (-1,1):
        belt(length+.30,side*track_y,.47 if heavy else .42)
        box('TrackFender',(length+.12,.30,.065),(0,side*track_y,.595 if heavy else .55),'armour',.017)
        for i in range(5):
            x=(i-2)*(length*.88/5)
            box('ModularSkirt',(length*.82/5,.065,.20),(x,side*(track_y+.166),.48),'team' if i==2 else 'edge',.012)
        box('FrontLamp',(.025,.11,.05),(length/2-.035,side*width*.36,.52),'mark',0)
        box('TopRecognition',(.58,.13,.025),(-.15,side*(width/2-.1),top+.012),'team',.004)
    vents((-length*.33,0,top+.012),(length*.20,width*.53),count=5)


def wheelset(xs, halfwidth, radius=.35):
    for x in xs:
        for side in (-1,1):
            cyl('Tyre',radius,.265,(x,side*halfwidth,radius),'dark',14,(90,0,0))
            cyl('WheelRim',radius*.58,.022,(x,side*(halfwidth+.145),radius),'metal',10,(90,0,0))
            cyl('Hub',radius*.23,.026,(x,side*(halfwidth+.155),radius),'edge',8,(90,0,0))


def truck_cab(x, width=1.02, roof=1.47, length=.74):
    octagon('ArmouredCab',length,width,.63,roof,center=(x,0),inset=.09)
    box('Windshield',(.025,width*.78,.25),(x+length*.46,0,roof-.24),'glass',.006)
    box('WindowPillar',(.04,.035,.30),(x+length*.475,0,roof-.22),'metal',0)
    for s in (-1,1):
        box('SideWindow',(length*.44,.018,.26),(x-.025,s*(width*.455),roof-.245),'glass',.008)
        box('DoorMark',(length*.49,.023,.16),(x-.02,s*(width*.482),.85),'team',.006)
        box('DoorHandle',(.11,.03,.027),(x-.17,s*(width*.497),1.01),'metal',0)
    box('CabRoof',(length*.79,width*.80,.035),(x,0,roof+.018),'team',.005)
    box('Grille',(.035,width*.55,.16),(x+length*.50,0,.79),'dark',.004)
    for y in (-.20,-.10,0,.10,.20): box('GrilleBars',(.012,.023,.13),(x+length*.524,y,.795),'metal',0)
    box('Bumper',(.11,width+.03,.105),(x+length*.46,0,.61),'metal',.009)


def tank(name,length,width,ty,tl,tw,gunlen,twin=False):
    begin(); tracked_hull(length,width,ty,heavy=twin)
    hull=bake_join('Hull'); snap=om.new_parts()
    cyl('TurretBearing',tw*.45,.10,(-.15,0,.84),'dark',16)
    octagon('CastTurret',tl,tw,.86,1.20,center=(-.15,0),inset=.16)
    for s in (-1,1):
        prism('ForwardCheek',[(tl*.31,s*.16),(tl*.29,s*tw*.48),(-.32,s*tw*.49),(-.39,s*.23)],.91,1.22,'edge',.07)
        box('TurretColour',(tl*.44,.11,.03),(-.23,s*tw*.35,1.225),'team',.004)
        box('AmmoBin',(tl*.27,.17,.20),(-tl*.41,s*tw*.40,1.065),'armour',.011)
    for y in ((-.22,.22) if twin else (0,)):
        start=tl/2-.2; endx=start+gunlen
        box('Mantlet',(.25,.24,.22),(start,y,1.08),'dark',.022)
        cyl('ArmouredSleeve',.088,.34,(start+.19,y,1.08),'edge',10,(0,90,0))
        cyl('GunTube',.060 if twin else .052,gunlen-.19,(start+(gunlen+.15)/2,y,1.08),'metal',10,(0,90,0))
        cyl('FumeExtractor',.084,.16,(start+gunlen*.54,y,1.08),'armour',10,(0,90,0))
        cyl('Muzzle',.074,.12,(endx-.07,y,1.08),'metal',10,(0,90,0))
        cyl('Bore',.048,.008,(endx-.006,y,1.08),'dark',10,(0,90,0))
    cyl('CommanderHatch',.17,.055,(-.37,.15,1.245),'dark',12)
    cyl('HatchPlate',.139,.031,(-.37,.15,1.286),'edge',10)
    box('Periscope',(.13,.14,.10),(-.26,-.16,1.264),'metal',.006)
    box('Sight',(.012,.095,.051),(-.19,-.16,1.272),'glass',0)
    if twin:
        vents((-.67,0,1.226),(.30,.66),count=5)
        for s in (-1,1):
            for x in (-.61,-.48): cyl('SmokePod',.043,.17,(x,s*.55,1.17),'metal',8,(s*22,30,0))
    turret=bake_join('Turret',om.parts_since(snap),(-.15,0,.85)); om.parent(turret,hull)
    end(name)


def vanguard(): tank('vanguard',2.0,1.2,.75,1.0,.90,1.3)
def colossus(): tank('colossus',3.0,1.8,1.05,1.6,1.4,1.8,True)


def engineering_vehicle():
    begin(); tracked_hull(1.60,1.13,.66,top=.80)
    truck_cab(-.38,.91,1.40,.68)
    # Split heavy blade, reinforced cutting edge, and twin hydraulic push arms.
    prism('DozerBlade',[(.94,-.81),(1.06,-.75),(1.06,.75),(.94,.81)],.12,.80,'metal',0)
    box('CuttingEdge',(.09,1.66,.085),(1.027,0,.15),'edge',0)
    for s in (-1,1):
        beam('PushArm',(.20,s*.58,.41),(.95,s*.68,.45),.09)
        beam('HydraulicRam',(.0,s*.58,.66),(.85,s*.60,.60),.055,'edge')
        cyl('BladeHinge',.07,.06,(.92,s*.70,.40),'dark',10,(90,0,0))
        box('WarningPatch',(.021,.22,.085),(1.073,s*.54,.68),'mark',0)
    cyl('CraneBase',.16,.17,(-.93,.33,.94),'metal',12)
    beam('CraneUpright',(-.95,.34,1.0),(-1.10,.34,1.73),.13,'armour')
    beam('Boom',(-1.10,.34,1.73),(.05,.34,1.93),.13,'edge')
    beam('BoomRam',(-1.0,.34,1.28),(-.34,.34,1.80),.065)
    beam('HoistCable',(.0,.34,1.87),(.0,.34,1.55),.015,'dark')
    box('Hook',(.07,.045,.085),(.0,.34,1.54),'mark',0)
    # Godot treats the `_vehicle` suffix as a physics-body import hint. A static
    # art root must avoid it so this presentation model cannot fall under gravity.
    bake_join('EngineeringHull'); end('engineering_vehicle')


def supply_truck():
    begin(); box('Chassis',(2.53,1.06,.24),(-.02,0,.55),'dark',.01)
    wheelset((.9,-.2,-1.0),.60)
    truck_cab(.99,1.08,1.53,.78)
    box('CargoBed',(1.63,1.15,.15),(-.49,0,.76),'metal',.011)
    # A ribbed cargo body, hard corners, top canvas and rear loading doors.
    octagon('CargoBody',1.58,1.12,.80,1.68,center=(-.49,0),mat='edge',inset=.07)
    box('CanvasRoof',(1.45,1.04,.11),(-.49,0,1.733),'armour',.024)
    for side in (-1,1):
        for x in (-1.11,-.72,-.32,.09): box('CargoRib',(.045,.035,.79),(x,side*.565,1.19),'metal',0)
        box('LogisticsStripe',(.93,.02,.13),(-.50,side*.587,1.24),'team',.005)
        box('FootRail',(1.57,.045,.045),(-.5,side*.615,.70),'metal',0)
    box('RearDoors',(.033,1.02,.72),(-1.298,0,1.17),'armour',.003)
    for y in (-.36,.36): beam('DoorBar',(-1.321,y,.89),(-1.321,y,1.46),.022,'edge')
    bake_join('supply_truck'); end('supply_truck')


def infantry(name,role):
    begin(True)
    # Anatomical volumes, knees, webbing and tools read as an equipped soldier.
    for side in (-1,1):
        y=side*.125
        box('Boot',(.27,.195,.115),(.015,y,.063),'dark',.017)
        box('Shin',(.19,.165,.25),(-.012,y,.244),'armour',.015,rot=(0,side*3,0))
        box('Thigh',(.20,.183,.28),(-.005,y,.481),'armour',.019)
        box('KneePad',(.032,.142,.105),(.104,y,.38),'edge',.009)
    octagon('Coat',.335,.485,.58,.84,mat='armour',inset=.06)
    octagon('Torso',.32,.455,.77,1.063,mat='team',inset=.12)
    box('ChestRig',(.075,.355,.235),(.146,0,.905),'dark',.016)
    for y in (-.12,0,.12): box('MagazinePouch',(.048,.085,.115),(.182,y,.88),'edge',.006)
    box('Belt',(.338,.479,.05),(.0,0,.694),'dark',.004)
    box('BackEquipment',(.055,.315,.22),(-.148,0,.87),'metal',.014)
    for s in (-1,1):
        box('UpperArm',(.21,.135,.18),(.074,s*.277,.99),'armour',.018,rot=(0,20,0))
        box('Forearm',(.245,.122,.125),(.285,s*.277,.943),'armour',.011)
        box('Glove',(.105,.109,.10),(.425,s*.274,.942),'dark',.009)
        box('ShoulderPatch',(.118,.026,.09),(.055,s*.354,1.04),'team',.006)
    cyl('Neck',.063,.075,(0,0,1.097),'concrete',8)
    octagon('Head',.215,.223,1.115,1.304,center=(.012,0),mat='concrete',inset=.15)
    if role=='hacker':
        octagon('FieldCap',.285,.278,1.303,1.386,mat='dark',inset=.20)
        box('CapVisor',(.12,.233,.027),(.087,0,1.322),'dark',.006)
        box('Eyepiece',(.018,.18,.056),(.121,0,1.239),'glass',.004)
        box('RuggedTerminal',(.29,.385,.034),(.35,0,.961),'metal',.006)
        box('ScreenCase',(.033,.381,.226),(.531,0,1.075),'dark',.007)
        box('TerminalScreen',(.013,.313,.171),(.509,0,1.084),'glass',.004)
        for yy in (-.10,0,.10): box('Keyboard',(.13,.045,.009),(.39,yy,.983),'edge',0)
        cyl('RadioAerial',.009,.25,(-.139,-.105,1.10),'dark',6)
    else:
        octagon('Helmet',.285,.295,1.276,1.40,mat='armour',inset=.26)
        box('HelmetBand',(.265,.30,.028),(.0,0,1.315),'team',.004)
        box('FaceShadow',(.018,.125,.045),(.122,0,1.21),'dark',0)
        if role=='rifle':
            box('RifleReceiver',(.39,.069,.097),(.363,.141,.959),'metal',.006)
            cyl('RifleBarrel',.023,.33,(.576,.141,.966),'dark',8,(0,90,0))
            box('Magazine',(.094,.058,.133),(.335,.141,.866),'dark',.004,rot=(0,-12,0))
            box('Stock',(.18,.064,.083),(.074,.141,.960),'edge',.007)
            box('RifleSight',(.075,.034,.044),(.42,.141,1.027),'dark',.004)
        else:
            cyl('LauncherTube',.075,.965,(.185,.31,1.045),'armour',10,(0,90,0))
            for x in (-.248,.54): cyl('TubeBand',.081,.063,(x,.31,1.045),'metal',10,(0,90,0))
            cyl('OpenLauncher',.055,.013,(.673,.31,1.045),'dark',10,(0,90,0))
            box('OpticMount',(.11,.062,.12),(.32,.25,1.139),'dark',.006)
            box('ShoulderHarness',(.18,.05,.083),(.12,-.312,1.03),'mark',.007)
    bake_join(name); end(name,3000)


def conscript(): infantry('conscript','rifle')
def rocket_squad(): infantry('rocket_squad','rocket')
def hacker(): infantry('hacker','hacker')


def typhoon():
    begin(); tracked_hull(2.30,1.27,.80,top=.75)
    truck_cab(.83,1.08,1.29,.64)
    hull=bake_join('Hull'); snap=om.new_parts()
    cyl('LaunchBearing',.40,.11,(-.5,0,.88),'dark',14)
    for s in (-1,1):
        box('ElevationFork',(.46,.095,.40),(-.60,s*.42,1.045),'metal',.015)
        cyl('ElevationPin',.103,.16,(-.51,s*.44,1.17),'edge',10,(90,0,0))
    # Nine individually collared rocket cells, sharing the sloped launcher cradle.
    parts=[]; before=om.new_parts()
    box('PodShell',(1.52,1.04,.67),(-.5,0,1.40),'armour',.035)
    box('LaunchFace',(.055,1.005,.61),(.28,0,1.40),'dark',.008)
    for y in (-.325,0,.325):
        for z in (1.205,1.40,1.595):
            cyl('TubeCollar',.086,.07,(.29,y,z),'metal',10,(0,90,0))
            cyl('RocketCell',.062,.077,(.298,y,z),'dark',10,(0,90,0))
    for s in (-1,1): box('PodStripe',(.92,.023,.13),(-.5,s*.529,1.45),'team',.004)
    box('PodRoof',(1.31,.91,.03),(-.51,0,1.745),'edge',.005)
    # Bake a fixed elevation into mesh coordinates, leaving the Turret rest rotation identity.
    pivot=Vector((-.5,0,1.40)); from mathutils import Matrix
    rotation=Matrix.Rotation(math.radians(-27),4,'Y')
    for obj in om.parts_since(before): obj.matrix_world=Matrix.Translation(pivot)@rotation@Matrix.Translation(-pivot)@obj.matrix_world
    turret=bake_join('Turret',om.parts_since(snap),(-.5,0,.85)); om.parent(turret,hull)
    end('typhoon')


def hailstorm():
    begin(); tracked_hull(2.0,1.30,.80,top=.76)
    hull=bake_join('Hull'); snap=om.new_parts()
    cyl('Bearing',.41,.12,(-.1,0,.86),'dark',14)
    octagon('GunCradle',.94,1.00,.89,1.24,center=(-.10,0),inset=.13)
    for s in (-1,1):
        box('AmmoMagazine',(.56,.18,.29),(-.10,s*.425,1.04),'team',.017)
        cyl('CannonBreech',.095,.31,(.30,s*.29,1.14),'dark',10,(0,70,0))
        cyl('Autocannon',.054,1.30,(.585,s*.29,1.19),'metal',10,(0,70,0))
        cyl('BarrelShroud',.079,.38,(.32,s*.29,1.09),'edge',10,(0,70,0))
    box('RadarPedestal',(.13,.17,.18),(-.37,0,1.29),'dark',.004)
    octagon('RadarBack',.28,.63,1.36,1.53,center=(-.41,0),mat='metal',inset=.06)
    box('RadarFace',(.018,.54,.11),(-.258,0,1.447),'glass',.005)
    for y in (-.18,0,.18): box('RadarElement',(.018,.023,.115),(-.243,y,1.447),'edge',0)
    turret=bake_join('Turret',om.parts_since(snap),(-.1,0,.85)); om.parent(turret,hull)
    end('hailstorm')


def speaker():
    begin(); box('Chassis',(2.12,1.08,.24),(0,0,.56),'dark',.01); wheelset((.7,-.7),.58)
    truck_cab(.685,1.015,1.47,.76)
    octagon('AmplifierBody',1.14,1.04,.74,1.51,center=(-.42,0),mat='armour',inset=.04)
    vents((-.48,0,1.532),(.76,.74),count=7)
    for s in (-1,1):
        box('AudioCabinet',(.84,.03,.35),(-.45,s*.515,1.15),'dark',.003)
        for x in (-.72,-.45,-.18): cyl('SpeakerCone',.095,.034,(x,s*.543,1.16),'metal',12,(90,0,0))
        beam('HornSupport',(-.52,s*.27,1.45),(-.52,s*.42,1.78),.055)
        box('HornCasing',(.49,.32,.25),(-.48,s*.54,1.77),'edge',.026)
        box('HornMouth',(.365,.017,.16),(-.46,s*.708,1.79),'dark',.004)
    box('BannerFrame',(.89,.048,.44),(-.44,0,1.94),'metal',.005)
    box('BroadcastBanner',(.78,.053,.32),(-.44,0,1.965),'team',.004)
    for x in (-.77,-.11): box('BannerPost',(.032,.043,.50),(x,0,1.925),'metal',0)
    bake_join('speaker'); end('speaker')


def ghost_van():
    begin(); box('Chassis',(2.17,1.055,.20),(0,0,.51),'dark',.01); wheelset((.75,-.75),.58)
    octagon('SignalVan',2.26,1.09,.55,1.38,mat='armour',inset=.06)
    box('Windscreen',(.026,.84,.28),(1.075,0,1.14),'glass',.012)
    box('Divider',(.028,.035,.29),(1.094,0,1.14),'metal',0)
    for s in (-1,1):
        box('CabWindow',(.36,.024,.24),(.81,s*.506,1.145),'glass',.008)
        box('LowStripe',(1.98,.025,.108),(-.02,s*.551,.85),'team',.005)
        box('EquipmentPanel',(.67,.018,.31),(-.47,s*.541,1.13),'metal',.006)
        for x in (-.69,-.54,-.39,-.24): box('SideLouvre',(.055,.022,.22),(x,s*.556,1.14),'dark',0)
    vents((-.55,.11,1.395),(.75,.44),count=6)
    cyl('DishMount',.12,.20,(-.47,-.01,1.49),'metal',10)
    cyl('SignalDish',.39,.064,(-.47,-.01,1.645),'dark',16,(24,0,0))
    cyl('DishFace',.31,.021,(-.47,-.029,1.681),'edge',16,(24,0,0))
    beam('FeedArm',(-.47,-.01,1.68),(-.47,-.19,1.90),.021)
    box('Feed',(.065,.065,.08),(-.47,-.19,1.89),'glass',.005)
    cyl('RadioMast',.018,.96,(.44,.30,1.90),'dark',8)
    for z in (2.00,2.19): box('AntennaCrossbar',(.27,.025,.023),(.44,.30,z),'metal',0)
    bake_join('ghost_van'); end('ghost_van')


def troop_crawler():
    begin(); tracked_hull(2.48,1.40,.85,top=.81)
    octagon('TroopCompartment',2.02,1.30,.81,1.55,center=(-.20,0),inset=.13)
    for s in (-1,1):
        box('ArmourRail',(1.71,.06,.16),(-.22,s*.641,1.05),'team',.007)
        for x in (-.87,-.44,-.01,.42):
            box('VisionPort',(.245,.024,.084),(x,s*.595,1.387),'dark',.004)
            box('PortLens',(.145,.027,.037),(x,s*.610,1.389),'glass',0)
        box('GrabRail',(1.56,.024,.024),(-.24,s*.648,1.26),'metal',0)
    box('RearRamp',(.075,1.15,.72),(-1.32,0,1.06),'metal',.016)
    for z in (.81,1.0,1.19,1.38): box('RampStep',(.024,.91,.028),(-1.365,0,z),'edge',0)
    for x in (-.65,-.02):
        cyl('TroopHatch',.225,.038,(x,0,1.573),'dark',12)
        cyl('HatchPlate',.192,.027,(x,0,1.608),'edge',12)
    box('RemoteMount',(.18,.22,.095),(.61,.27,1.60),'dark',.007)
    box('MachineGun',(.29,.10,.093),(.79,.27,1.66),'metal',.007)
    cyl('MachineGunBarrel',.03,.39,(1.003,.27,1.68),'dark',8,(0,90,0))
    bake_join('troop_crawler'); end('troop_crawler')


def talon():
    begin()
    # Compact strike jet: tapered centre body, swept delta wings, paired intakes.
    sections=[(-1.54,.17,.66,.95),(-1.02,.38,.51,1.10),(.25,.37,.54,1.10),(1.22,.07,.64,.77)]
    vs=[]
    for x,w,l,h in sections: vs += [(x,-w*.7,l),(x,w*.7,l),(x,w,h-.10),(x,w*.45,h),(x,-w*.45,h),(x,-w,h-.10)]
    fs=[tuple(reversed(range(6))),tuple(range(18,24))]
    for ring in range(3):
        for j in range(6): fs.append((ring*6+j,ring*6+(j+1)%6,(ring+1)*6+(j+1)%6,(ring+1)*6+j))
    raw('ArmouredFuselage',vs,fs,'edge')
    for s in (-1,1):
        prism('SweptWing',[(.32,s*.22),(-.64,s*2.0),(-1.14,s*1.74),(-.71,s*.28)],.704,.778,'armour',.0)
        prism('WingRecognition',[(-.43,s*.88),(-.68,s*1.72),(-.95,s*1.59),(-.76,s*.83)],.780,.787,'team',.0)
        prism('LeadingEdge',[(-.33,s*1.48),(-.61,s*1.97),(-.675,s*1.95),(-.415,s*1.44)],.780,.791,'edge',.0)
        prism('ControlSurfaceRecess',[(-.73,s*.42),(-1.08,s*1.69),(-1.11,s*1.68),(-.76,s*.42)],.780,.786,'dark',.0)
        prism('Aileron',[(-.78,s*.48),(-1.1,s*1.66),(-1.145,s*1.70),(-.825,s*.48)],.781,.806,'metal',.0)
        octagon('WingRootFairing',.74,.23,.64,.88,(-.24,s*.36),'edge',.27)
        prism('Tailplane',[(-1.08,s*.15),(-1.61,s*.83),(-1.63,s*.23)],.889,.944,'armour',.0)
        prism('TailControl',[(-1.55,s*.31),(-1.58,s*.75),(-1.63,s*.78),(-1.64,s*.30)],.943,.953,'metal',.0)
        box('Intake',(.38,.18,.24),(.06,s*.39,.77),'dark',.012)
        box('IntakeLip',(.047,.20,.265),(.268,s*.39,.78),'metal',.007)
        for z in (.705,.775,.845): box('IntakeSplitter',(.012,.135,.018),(.295,s*.39,z),'edge',0)
        cyl('StrikePod',.080,.62,(-.18,s*.58,.545),'metal',16,(0,90,0))
        cyl('WeaponCollar',.087,.055,(.080,s*.58,.545),'armour',12,(0,90,0))
        cyl('PodNose',.064,.06,(.145,s*.58,.545),'mark',12,(0,90,0))
        cyl('WeaponTail',.058,.10,(-.525,s*.58,.545),'dark',12,(0,90,0))
        for a in (0,90,180,270):
            box('WeaponFin',(.19,.022,.16),(-.422,s*.58,.545),'edge',.004,rot=(a,0,0))
        box('Pylon',(.31,.064,.13),(-.17,s*.57,.666),'dark',.004)
        box('PylonBrace',(.22,.10,.040),(-.17,s*.57,.72),'metal',.006)
    # Long, low armoured cockpit with a raked windshield, rather than a box canopy.
    cv=[]
    for x,w,l,h in ((.97,.065,.90,.984),(.72,.197,.97,1.165),(.33,.192,1.035,1.225),(.16,.071,1.044,1.136)):
        cv += [(x,-w,l),(x,w,l),(x,w*.64,h),(x,-w*.64,h)]
    cf=[(3,2,1,0),(12,13,14,15)]
    for j in range(3):
        for i in range(4): cf.append((j*4+i,j*4+(i+1)%4,(j+1)*4+(i+1)%4,(j+1)*4+i))
    raw('RakedCanopy',cv,cf,'glass')
    for s in (-1,1):
        beam('CanopyRail',(.97,s*.064,.911),(.33,s*.192,1.045),.024,'metal')
        beam('WindscreenFrame',(.72,s*.197,.98),(.72,s*.126,1.17),.021,'metal')
    beam('CanopySpine',(.965,0,.989),(.331,0,1.233),.027,'metal')
    vents((-.54,0,1.109),(.49,.28),count=7)
    for s in (-1,1):
        box('SpinePanel',(.26,.13,.023),(-.95,s*.12,1.105),'metal',.005)
    raw('VerticalFin',[(-1.47,-.035,.86),(-.97,-.035,.90),(-1.32,-.035,1.44),(-1.55,-.035,1.42),
                       (-1.47,.035,.86),(-.97,.035,.90),(-1.32,.035,1.44),(-1.55,.035,1.42)],
                      [(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],'team')
    cyl('ExhaustShield',.217,.19,(-1.489,0,.80),'armour',20,(0,90,0))
    cyl('ExhaustCollar',.218,.055,(-1.588,0,.80),'edge',20,(0,90,0))
    cyl('Exhaust',.199,.105,(-1.626,0,.80),'metal',20,(0,90,0))
    cyl('Nozzle',.156,.018,(-1.690,0,.80),'dark',20,(0,90,0))
    for i in range(12):
        a=i*math.tau/12
        box('NozzlePetal',(.105,.04,.019),(-1.634,.188*math.sin(a),.80+.188*math.cos(a)),
            'edge',.003,rot=(-math.degrees(a),0,0))
    bake_join('talon'); end('talon')


UNITS={name:globals()[name] for name in ('engineering_vehicle','supply_truck','conscript','rocket_squad','hacker',
    'vanguard','colossus','typhoon','hailstorm','speaker','ghost_van','troop_crawler','talon')}


def foundation(width,depth,height=.20):
    box('Foundation',(width-.30,depth-.30,height),(0,0,height/2),'metal',.035)
    box('ConcreteApron',(width-.39,depth-.39,.034),(0,0,height+.017),'concrete',.008)
    # Expansion joints and inset corner protection belong to the slab, not the map.
    for s in (-1,1):
        box('FoundationSeam',(width-.46,.018,.008),(0,s*(depth*.28),height+.038),'dark',0)
        for x in (-(width-.55)/2,(width-.55)/2):
            box('CornerSteel',(.18,.18,.048),(x,s*(depth-.55)/2,height+.025),'dark',.010)


def shell(center,size,bottom=.24,mat='concrete',roof=True):
    x,y=center; sx,sy,h=size
    octagon('ReinforcedStructure',sx,sy,bottom,bottom+h,(x,y),mat,.035)
    box('BaseCourse',(sx+.045,sy+.045,.17),(x,y,bottom+.07),'armour',.018)
    if roof:
        box('RoofOverhang',(sx+.09,sy+.09,.105),(x,y,bottom+h+.055),'metal',.025)
        box('RoofField',(sx-.14,sy-.14,.035),(x,y,bottom+h+.125),'dark',.008)
    for s in (-1,1):
        for yy in (-.35,.35):
            box('ConcreteRib',(.12,.15,h*.82),(x+s*(sx/2-.04),y+yy*sy,bottom+h*.44),'edge',.012)


def portal(center,width,height,axis='x',rolling=True):
    x,y,z=center
    def part(name,dims,loc,mat,bevel=.006):
        dx,u,dz=loc; thick,w,h=dims
        return box(name,(thick,w,h) if axis=='x' else (w,thick,h),
                   (x+dx,y+u,z+dz) if axis=='x' else (x+u,y-dx,z+dz),mat,bevel)
    part('DoorFrame',(.08,width+.18,height+.12),(0,0,0),'metal',.014)
    part('DoorRecess',(.084,width,height),( .012,0,0),'dark',.006)
    if rolling:
        for i in range(6): part('RollerShutter',(.024,width-.06,height*.11),(.064,0,(i-2.5)*height/6),'armour',.002)
    else:
        for s in (-1,1):
            part('BlastDoor',(.024,width*.43,height*.93),(.065,s*width*.24,0),'armour',.007)
            part('DoorStiffener',(.027,width*.39,.065),(.080,s*width*.24,height*.22),'edge',.002)
            part('LockBar',(.040,.035,height*.30),(.097,s*.055,0),'metal',0)
    part('HeaderColour',(.024,width+.11,.12),(.048,0,height*.5+.08),'team',.003)


def roof_team(x,y,sx,sy,z):
    box('RoofRecognition',(sx,sy,.025),(x,y,z),'team',.006)
    for s in (-1,1): box('RecognitionGuard',(.038,sy+.08,.033),(x+s*(sx/2+.035),y,z+.005),'metal',0)


def pipe(name,a,b,r=.045,mat='metal'):
    a=Vector(a);b=Vector(b)
    rotation=(b-a).to_track_quat('Z','Y').to_euler()
    return cyl(name,r,(b-a).length,(a+b)/2,mat,10,tuple(math.degrees(v) for v in rotation))


def dish(center,radius=.44,tilt=25):
    x,y,z=center
    cyl('DishSupport',radius*.19,.24,(x,y,z-.14),'metal',10)
    cyl('DishBack',radius,.09,(x,y,z),'metal',16,(tilt,0,0))
    cyl('DishReflector',radius*.84,.025,(x,y-.026,z+.047),'edge',16,(tilt,0,0))
    beam('FeedArm',(x,y,z+.06),(x,y-.14,z+.32),.027)
    box('DishFeed',(.065,.065,.08),(x,y-.14,z+.31),'glass',.006)


def command_bunker():
    begin(); foundation(5,5)
    octagon('HeavyCommandCasemate',3.82,3.37,.25,1.49,mat='concrete',inset=.12)
    box('ArmouredRoof',(3.37,3.05,.16),(0,0,1.49),'armour',.03)
    # Angled frontal buttresses protect a recessed two-leaf blast entrance.
    for s in (-1,1):
        prism('FrontButtress',[(1.46,s*.64),(2.25,s*.80),(2.25,s*1.35),(1.42,s*1.55)],.24,1.27,'edge',.12)
        box('CornerTeamPlate',(.032,.39,.48),(1.93,s*.99,.95),'team',.008)
        box('ObservationSlit',(1.38,.022,.125),(-.33,s*1.56,1.10),'dark',.006)
        for x in (-.88,-.43,.02): box('SlitDivider',(.035,.029,.125),(x,s*1.573,1.10),'metal',0)
    portal((1.74,0,.77),1.05,.95,rolling=False)
    for i in range(3): box('EntranceStep',(.23,1.16,.08),(2.13-i*.19,0,.24+i*.06),'armour',.008)
    shell((-.38,0),(2.18,1.97,.79),1.60)
    for s in (-1,1):
        box('CommandWindow',(1.40,.025,.21),(-.42,s*.968,2.08),'glass',.006)
        for x in (-.85,-.39,.07): box('WindowMullion',(.037,.036,.24),(x,s*.982,2.08),'metal',0)
    roof_team(-.38,0,1.12,.40,2.54)
    vents((-.88,.57,2.555),(.61,.49),count=5)
    # Lattice radio mast with paired aerials and a recognisable banner.
    for y in (-1.04,-.82): beam('MastLeg',(-1.14,y,1.48),(-1.14,y,4.47),.038)
    for i in range(5):
        z=1.60+i*.53
        beam('MastBrace',(-1.14,-1.04,z),(-1.14,-.82,z+.49),.022)
    for z in (3.40,4.00): box('AerialCrossbar',(.68,.045,.032),(-1.14,-.93,z),'metal',0)
    box('DirectorateBanner',(.62,.027,.39),(-.81,-.93,4.26),'team',.006)
    dish((.61,.66,2.90),.48,40)
    bake_join('command_bunker'); end('command_bunker',10000)


def reactor():
    begin(); foundation(3,3)
    shell((-.66,.07),(1.10,2.05,.93),.24)
    portal((-.67,-.98,.68),.58,.71,axis='y',rolling=False)
    roof_team(-.71,.08,.47,1.33,1.322)
    # Stepped containment drum and radial reinforcing ribs around the core.
    cyl('ContainmentFoot',.84,.22,(.51,0,.37),'metal',16)
    cyl('ContainmentVessel',.75,1.11,(.51,0,1.01),'concrete',20)
    cyl('ContainmentShoulder',.67,.18,(.51,0,1.58),'armour',20)
    cyl('ReactorCrown',.49,.27,(.51,0,1.805),'edge',16)
    cyl('TopShield',.40,.12,(.51,0,2.005),'metal',16)
    cyl('TopIndicator',.24,.025,(.51,0,2.079),'team',12)
    for i in range(10):
        a=i*math.tau/10
        x=.51+math.cos(a)*.725;y=math.sin(a)*.725
        box('ContainmentRib',(.09,.09,1.15),(x,y,1.00),'armour',.01)
    for z in (.62,1.12): cyl('ReinforcingBand',.768,.08,(.51,0,z),'metal',20)
    cyl('Stack',.155,1.58,(-1.035,-.86,1.47),'dark',12)
    cyl('StackShield',.235,.26,(-1.035,-.86,2.19),'metal',12)
    cyl('StackOpening',.164,.014,(-1.035,-.86,2.326),'dark',12)
    for yy in (-.64,.61):
        pipe('CoolingLoop',(-.28,yy,.52),(.43,yy,.52),.071,'metal')
        pipe('CoolingRiser',(.43,yy,.52),(.43,yy,1.30),.071,'metal')
    box('HazardCabinet',(.30,.30,.39),(.82,1.01,.44),'mark',.023)
    box('CabinetInset',(.20,.015,.23),(.82,1.168,.47),'dark',.004)
    bake_join('reactor'); end('reactor',10000)


def barracks():
    begin(); foundation(3,3)
    shell((0,.23),(2.38,1.73,1.0),.24,roof=False)
    prism('ReinforcedRoof',[(-1.25,-.69),(1.25,-.69),(1.25,1.15),(-1.25,1.15)],1.24,1.68,'armour',.10)
    roof_team(-.15,.18,1.33,.42,1.704)
    for x in (-.85,0,.85): box('RoofRib',(.057,1.71,.075),(x,.23,1.701),'metal',.006)
    portal((1.197,-.03,.71),.70,.78,rolling=False)
    for x in (-.72,0,.72):
        box('BarrackWindow',(.40,.025,.24),(x,-.642,.985),'dark',.004)
        box('WindowPane',(.315,.012,.14),(x,-.660,.993),'glass',.003)
        box('WindowGuard',(.022,.026,.20),(x,-.674,.99),'metal',0)
    # A sandbag parapet surrounding the drill apron, with individual staggered sacks.
    box('DrillApron',(2.35,.58,.035),(-.02,-1.014,.251),'armour',.005)
    for row in range(2):
        for i in range(6):
            x=-.995+i*.375+(row%2)*.045
            box('Sandbag',(.345,.235,.145),(x,-1.294,.325+row*.127),'edge',.044)
    for x in (-.88,.75):
        box('KitCrate',(.30,.24,.26),(x,-.86,.403),'metal',.012)
        box('CrateStripe',(.05,.247,.265),(x,-.86,.403),'team',.003)
    vents((-.78,.72,1.70),(.44,.44),count=4)
    bake_join('barracks'); end('barracks',10000)


def supply_depot():
    begin(); foundation(4,4)
    shell((-.58,.0),(2.30,3.10,1.39),.25,roof=False)
    # A long gabled loading hall with ribs and a raised goods dock.
    raw('PitchedRoof',[(-1.82,-1.64,1.62),(.66,-1.64,1.62),(.66,0,2.17),(-1.82,0,2.17),
                        (-1.82,1.64,1.62),(.66,1.64,1.62)],
                       [(0,1,2,3),(3,2,5,4),(0,3,4),(1,5,2),(0,4,5,1)],'metal')
    for y in (-1.34,-.77,-.20,.37,.94,1.48):
        z=2.17-abs(y)*.335
        box('RoofStandingSeam',(2.30,.035,.027),(-.58,y,z+.015),'edge',0)
    roof_team(-.58,0,1.37,.27,2.185)
    box('LoadingDock',(1.04,2.96,.22),(1.18,0,.36),'armour',.025)
    for y in (-.83,.83): portal((.597,y,1.025),1.07,1.23)
    for y in (-1.32,0,1.32):
        box('DockBumper',(.12,.15,.22),(1.737,y,.403),'dark',.008)
        box('BumperMark',(.012,.12,.062),(1.802,y,.446),'mark',0)
    for x,y,z in ((1.12,-1.07,.76),(1.16,-.47,.76),(1.10,.84,.76),(1.11,-.77,1.22)):
        box('SupplyCrate',(.48,.45,.44),(x,y,z),'edge',.021)
        for s in (-1,1): box('CrateStrap',(.06,.464,.447),(x+s*.15,y,z),'metal',.002)
    for y in (-.82,.82): box('HallTeamPlate',(.027,.51,.34),(-1.744,y,1.11),'team',.004)
    bake_join('supply_depot'); end('supply_depot',10000)


def vehicle_plant():
    begin(); foundation(4,4)
    shell((-.02,.26),(3.02,2.80,1.52),.25)
    portal((0,-1.158,.95),2.10,1.30,axis='y')
    # Reinforced front portal and an overhead servicing crane distinguish the plant.
    for s in (-1,1):
        box('GantryColumn',(.23,.23,1.91),(s*1.46,-1.30,1.205),'edge',.022)
        box('PillarBand',(.235,.241,.34),(s*1.46,-1.30,1.40),'team',.006)
        box('GantryRail',(.10,2.79,.10),(s*1.42,.13,2.09),'metal',.006)
    box('CraneBeam',(2.97,.19,.19),(0,-.78,2.20),'mark',.010)
    box('Trolley',(.44,.40,.18),(.35,-.78,2.30),'metal',.010)
    beam('HoistCable',(.35,-.78,2.22),(.35,-.78,1.80),.025,'dark')
    box('CraneHook',(.11,.07,.14),(.35,-.78,1.81),'metal',.008)
    for x in (-.94,0,.94):
        cyl('StackPlinth',.235,.14,(x,1.29,1.96),'armour',12)
        cyl('ExhaustStack',.135,.73,(x,1.29,2.39),'metal',12)
        cyl('StackRim',.17,.075,(x,1.29,2.790),'edge',12)
        cyl('SootOpening',.12,.012,(x,1.29,2.834),'dark',12)
    vents((-.70,.28,1.913),(.98,.98),count=7)
    roof_team(.69,.26,.56,.97,1.914)
    for x in (-.54,.54): box('ServiceLane',(.07,.51,.012),(x,-1.58,.251),'mark',0)
    bake_join('vehicle_plant'); end('vehicle_plant',10000)


def airfield():
    begin(); foundation(6,4,.14)
    box('ArmouredRunway',(5.42,1.47,.035),(0,-.95,.188),'dark',.005)
    for x in (-2.21,-1.13,0,1.13,2.21): box('RunwayCenterline',(.52,.055,.009),(x,-.95,.211),'mark',0)
    for s in (-1,1):
        for x in (-2.47,-2.26,2.26,2.47): box('Threshold',(.09,.72,.009),(x,-.95,.213),'edge',0)
        for x in (-2.52,-1.27,0,1.27,2.52): box('RunwayLight',(.085,.08,.042),(x,-.95+s*.685,.219),'glass',.007)
    # A protected hangar with a faceted roof and inset split doors.
    shell((-1.48,1.09),(2.34,1.34,1.06),.19,roof=False)
    octagon('HangarRoof',2.44,1.44,1.24,1.69,(-1.48,1.09),'armour',.14)
    portal((-.29,1.09,.737),1.07,.93)
    for x in (-2.30,-1.76,-1.22,-.68): box('HangarRoofSeam',(.035,1.18,.024),(x,1.09,1.695),'metal',0)
    roof_team(-1.47,1.09,1.15,.39,1.714)
    shell((1.59,1.28),(.72,.72,2.07),.20)
    octagon('ControlCabin',1.05,1.05,2.24,2.70,(1.59,1.28),'metal',.05)
    for s in (-1,1):
        box('ControlWindowX',(.021,.77,.26),(1.59+s*.514,1.28,2.488),'glass',.005)
        box('ControlWindowY',(.77,.021,.26),(1.59,1.28+s*.514,2.488),'glass',.005)
    box('ControlRoof',(1.17,1.12,.075),(1.59,1.28,2.802),'team',.016)
    vents((.58,1.16,.32),(.50,.70),count=5)
    box('FuelCabinet',(.47,.71,.43),(.58,1.16,.44),'armour',.018)
    for y in (.94,1.34): cyl('FuelCap',.07,.045,(.58,y,.681),'mark',10)
    bake_join('airfield'); end('airfield',10000)


def propaganda_center():
    begin(); foundation(4,4)
    shell((0,0),(2.93,2.90,.90),.24)
    portal((0,-1.477,.68),.90,.70,axis='y',rolling=False)
    # A massive framed broadcast panel and horn arrays carry the building's role.
    for y in (-.91,.91): box('ScreenColumn',(.24,.20,1.66),(.99,y,1.475),'metal',.014)
    box('BroadcastFrame',(.22,2.57,1.44),(1.12,0,2.045),'edge',.025)
    box('ScreenRecess',(.034,2.35,1.23),(1.248,0,2.055),'dark',.004)
    box('BroadcastDisplay',(.018,2.18,1.075),(1.274,0,2.08),'team',.006)
    # Original geometric broadcast insignia rather than borrowed insignia or text.
    for s in (-1,1):
        box('DisplayChevron',(.017,.60,.065),(1.288,s*.24,2.04),'edge',0,rot=(s*29,0,0))
        box('DisplayMark',(.017,.045,.55),(1.289,s*.66,2.075),'edge',0)
    for z in (2.36,2.94):
        box('SpeakerBank',(.39,.42,.39),(-1.04,.88,z),'metal',.028)
        for s in (-1,1):
            cyl('HornRim',.145,.029,(-1.04,.88+s*.227,z),'edge',12,(90,0,0))
            cyl('HornCone',.114,.036,(-1.04,.88+s*.245,z),'dark',12,(90,0,0))
    cyl('BroadcastMast',.045,2.15,(-1.04,.88,2.50),'metal',8)
    beam('MastGuy',(-1.04,.88,3.25),(-.15,.91,1.30),.018,'dark')
    vents((-.61,-.45,1.284),(.89,.68),count=6)
    roof_team(-.36,.19,.77,.39,1.285)
    bake_join('propaganda_center'); end('propaganda_center',10000)


def cyber_center():
    begin(); foundation(3,3)
    shell((0,0),(2.13,2.13,1.42),.25,mat='armour')
    portal((1.087,-.12,.81),.66,1.02,rolling=False)
    for s in (-1,1):
        box('ServerWindow',(1.42,.023,.53),(-.03,s*1.039,1.19),'dark',.004)
        for x in (-.57,-.19,.19,.57):
            box('ServerRack',(.21,.028,.39),(x,s*1.06,1.19),'metal',.003)
            for z in (1.05,1.17,1.29): box('ServerStatus',(.105,.013,.021),(x,s*1.081,z),'glass',0)
        box('TeamFacade',(.48,.028,.19),(-.30,s*1.086,.59),'team',.004)
    for x in (-.66,0,.66): vents((x,.18,1.817),(.35,1.42),count=6,axis='y')
    dish((.54,-.47,2.27),.47,30)
    box('CoolingUnit',(.54,.50,.32),(-.66,.65,1.985),'edge',.012)
    cyl('CoolingFan',.17,.022,(-.66,.65,2.156),'dark',12)
    for a in (0,60,120): box('FanGrille',(.36,.02,.012),(-.66,.65,2.178),'metal',0,rot=(0,0,a))
    roof_team(-.45,-.70,.65,.29,1.814)
    bake_join('cyber_center'); end('cyber_center',10000)


def flak_tower():
    begin(); foundation(2.10,2.10)
    octagon('FlakBlockhouse',1.30,1.30,.25,1.73,mat='concrete',inset=.045)
    for s in (-1,1):
        box('CornerButtress',(.18,1.25,1.12),(s*.57,0,.83),'armour',.022)
        box('TeamPanel',(.025,.39,.24),(s*.674,0,1.32),'team',.007)
        box('ObservationSlit',(.025,.69,.105),(s*.683,0,1.05),'dark',.003)
    box('TopCap',(1.36,1.36,.095),(0,0,1.779),'metal',.02)
    portal((0,-.676,.72),.51,.85,axis='y',rolling=False)
    hull=bake_join('Hull');snap=om.new_parts()
    cyl('Bearing',.47,.13,(0,0,1.87),'dark',16)
    octagon('ArmouredMount',.66,.66,1.92,2.21,mat='armour',inset=.13)
    for s in (-1,1):
        box('Magazine',(.39,.19,.28),(-.06,s*.325,2.08),'team',.012)
        cyl('FlakBreech',.095,.29,(.24,s*.15,2.015),'metal',10,(0,60,0))
        cyl('FlakBarrel',.045,1.04,(.53,s*.15,2.19),'metal',10,(0,60,0))
        cyl('RecoilSleeve',.074,.28,(.30,s*.15,2.057),'edge',10,(0,60,0))
    cyl('GatlingCasing',.099,.39,(.34,0,1.965),'dark',12,(0,90,0))
    for a in (0,72,144,216,288):
        a=math.radians(a)
        cyl('GatlingBarrel',.023,.57,(.548,.060*math.cos(a),1.965+.06*math.sin(a)),'metal',6,(0,90,0))
    box('OpticalSight',(.14,.21,.12),(-.15,0,2.247),'metal',.007)
    box('SightLens',(.018,.145,.064),(-.07,0,2.255),'glass',0)
    turret=bake_join('Turret',om.parts_since(snap),(0,0,1.85));om.parent(turret,hull)
    end('flak_tower',10000)


def missile_silo():
    begin(); foundation(4,4)
    octagon('SiloBunker',2.93,2.90,.24,.95,mat='concrete',inset=.08)
    for s in (-1,1):
        box('SiloTeamBand',(1.72,.025,.15),(.22,s*1.433,.65),'team',.004)
        for x in (-.58,.24,1.03): box('BlastRib',(.17,.22,.61),(x,s*1.30,.56),'armour',.016)
    cyl('SiloRing',.98,.21,(.31,.06,1.03),'metal',20)
    cyl('SiloWell',.82,.07,(.31,.06,1.166),'dark',20)
    # Retracted protective lids, visible hinges and their hydraulic cylinders.
    for s in (-1,1):
        box('RetractedLid',(.99,.63,.11),(.31,s*.76,1.315),'armour',.015,rot=(s*34,0,0))
        for x in (-.09,.68):
            cyl('LidHinge',.095,.14,(x,s*.55,1.11),'edge',10,(0,90,0))
            beam('LidActuator',(x,s*.46,1.08),(x,s*1.00,1.47),.055)
        box('LidRecognition',(.57,.16,.028),(.31,s*.93,1.477),'team',.003,rot=(s*34,0,0))
    cyl('MissileBody',.257,1.22,(.31,.06,1.82),'edge',16)
    for z in (1.40,1.94,2.35): cyl('MissileBand',.263,.055,(.31,.06,z),'metal',16)
    # A tapered warhead, not the placeholder's blunt cylinder.
    n=16;vs=[]
    for z,r in ((2.415,.256),(2.66,.17),(2.825,.035)):
        vs += [(.31+r*math.cos(i*math.tau/n),.06+r*math.sin(i*math.tau/n),z) for i in range(n)]
    fs=[tuple(reversed(range(n))),tuple(range(2*n,3*n))]
    for j in range(2):
        for i in range(n):fs.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    raw('Warhead',vs,fs,'mark')
    for a in (0,90,180,270):
        a=math.radians(a)
        box('Stabiliser',(.44,.035,.30),(.31+.17*math.cos(a),.06+.17*math.sin(a),1.31),'metal',0,rot=(0,0,math.degrees(a)))
    shell((-1.16,-1.09),(.88,.89,1.05),.25)
    portal((-.704,-1.09,.83),.48,.86,rolling=False)
    box('ControlWindow',(.39,.018,.15),(-1.16,-1.548,1.095),'glass',.003)
    roof_team(-1.16,-1.09,.47,.30,1.444)
    bake_join('missile_silo');end('missile_silo',10000)


BUILDINGS={name:globals()[name] for name in ('command_bunker','reactor','barracks','supply_depot','vehicle_plant',
    'airfield','propaganda_center','cyber_center','flak_tower','missile_silo')}
