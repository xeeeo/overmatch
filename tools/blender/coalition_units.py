"""Coalition unit redesigns. +X forward, original runtime articulation pivots.

The approved Bulwark stays in build_bulwark.py. Dispatch entry points in the
original build_models/build_extras modules call these builders.
"""
import math
import bpy
import omlib as om
from coalition_art import *


def dozer():
    m=start()
    tracks(1.7,.615,.31,.40,m,x=-.1,bottom=.07)
    loft('EngineHull',[(-.92,.40,.37,.73),(-.7,.5,.37,.84),(.38,.46,.36,.69),(.68,.34,.37,.60)],m['armour'])
    facet('ArmouredCab',(.78,.77,.63),(-.36,0,1.08),m['edge'],taper=.2)
    box('CabWindscreen',(.018,.48,.23),(-.009,0,1.15),m['glass'],bevel=.004,rot=(0,-7,0))
    box('CabRoof',(.68,.68,.065),(-.4,0,1.383),m['team'])
    for y in (-.335,.335):
        box('CabSideWindow',(.52,.018,.22),(-.36,y*1.02,1.15),m['glass'],bevel=.004,rot=(7 if y>0 else -7,0,0))
        beam('CabPillar',(-.66,y,.97),(-.65,y,1.35),.045,m['armour'])
        beam('FrontPillar',(-.02,y,.97),(-.1,y,1.34),.05,m['armour'])
        box('Door',( .5,.025,.13),(-.36,y,.9),m['team'])
        rod('HydraulicArm',(.06,y*1.6,.46),(.90,y*1.6,.32),.05,m['steel'])
        rod('Ram',(.03,y*1.6,.63),(.81,y*1.6,.5),.022,m['marking'])
    # Broad concave blade, segmented cutting edge and angled reinforced wings.
    vertices=[(.89,-.78,.10),(.99,-.78,.72),(.87,-.52,.1),(.92,-.52,.72),
              (.87,.52,.1),(.92,.52,.72),(.89,.78,.10),(.99,.78,.72)]
    mesh('BladeFace',vertices,[(0,2,3,1),(2,4,5,3),(4,6,7,5)],m['utility'])
    box('BladeCuttingEdge',(.1,1.56,.09),(.93,0,.145),m['steel'],bevel=.008)
    for y in (-.68,-.34,0,.34,.68):
        beam('BladeRib',(.89,y,.2),(.94,y,.65),.05,m['edge'])
    vent((.28,0,.71),(.40,.5),m)
    cyl('Exhaust',.045,.39,(.28,-.29,1.0),m['steel'],verts=8)
    box('ExhaustCap',(.10,.10,.035),(.28,-.29,1.20),m['dark'])
    for s in (-1,1):
        box('TrackGuard',(1.51,.29,.08),(-.1,s*.625,.52),m['armour'])
        box('SafetyStripe',(.44,.30,.025),(.16,s*.625,.573),m['team'])
        lamp(.65,s*.3,.63,m)
    join('coalition_dozer'); finish('dozer')


def vehicle_base(m, length=2.2, wheel_x=(-.7,.7), wheel_y=.60, cab_x=-.15, cab_length=1.08):
    loft('ArmouredChassis',[(-length/2,.43,.32,.65),(-length*.35,.51,.32,.70),
                           (length*.35,.51,.32,.72),(length/2,.43,.34,.58)],m['dark'])
    for x in wheel_x:
        for s in (-1,1):
            wheel(x,s*wheel_y,.35,.34,.28,m)
            box('Fender',(.68,.32,.11),(x,s*wheel_y,.705),m['armour'])
    facet('CrewCell',(cab_length,1.03,.75),(cab_x,0,1.055),m['armour'],taper=.17)
    front=cab_x+cab_length*.5*(1-.17*(1.23-.68)/.75)+.012
    box('Windscreen',(.024,.66,.25),(front,0,1.23),m['glass'],bevel=.005,
        rot=(0,-math.degrees(math.atan(cab_length*.5*.17/.75)),0))
    box('Roof',(cab_length*.80,.80,.07),(cab_x-.025,0,1.454),m['edge'])
    for s in (-1,1):
        box('SideWindow',(cab_length*.64,.020,.23),(cab_x,s*.46,1.23),m['glass'],bevel=.004,rot=(s*6.65,0,0))
        box('DoorPanel',(cab_length*.64,.034,.26),(cab_x,s*.502,.94),m['team'],bevel=.008)
        beam('CabPillar',(cab_x+.37,s*.46,.94),(cab_x+.3,s*.4,1.40),.046,m['edge'])
        box('DoorHandle',(.11,.025,.026),(cab_x-.18,s*.524,1.05),m['steel'],bevel=0)


def warden():
    m=start(); vehicle_base(m)
    loft('Bonnet',[(.39,.48,.65,1.15),(.88,.48,.65,1.02),(1.17,.41,.58,.78)],m['armour'])
    facet('BonnetPlate',(.7,.7,.035),(.72,0,1.01),m['edge'],taper=0)
    box('FrontGrille',(.03,.59,.16),(1.185,0,.70),m['dark'],bevel=0)
    for y in (-.2,-.1,0,.1,.2): box('GrilleFin',(.038,.025,.14),(1.208,y,.70),m['steel'],bevel=0)
    for s in (-1,1):
        lamp(1.22,s*.39,.75,m)
        box('BumperWing',(.11,.25,.12),(1.21,s*.46,.54),m['steel'])
    vent((-.90,0,.77),(.32,.66),m)
    hull=join('Hull'); snap=om.new_parts()
    cyl('RemoteMount',.185,.1,(-.2,0,1.50),m['dark'])
    facet('RemoteCradle',(.42,.32,.14),(-.13,0,1.605),m['armour'],taper=.1)
    box('GunReceiver',(.38,.09,.1),(.02,0,1.64),m['steel'])
    cyl('GunBarrel',.026,.50,(.40,0,1.64),m['steel'],verts=8,rot=(0,90,0))
    cyl('Muzzle',.041,.08,(.69,0,1.64),m['dark'],verts=8,rot=(0,90,0))
    box('Sight',(.11,.085,.055),(-.09,-.15,1.64),m['glass'])
    box('AmmunitionCan',(.18,.11,.1),(-.24,.19,1.60),m['team'])
    articulate('Turret',(-.2,0,1.5),hull,snap); finish('warden')


def soldier(name, weapon):
    m=start()
    # An alert staggered stance, armoured torso and distinct lower legs.
    for s in (-1,1):
        x=.055*s; y=s*.14
        facet('Boot',(.29,.20,.12),(x+.025,y,.06),m['dark'],taper=.12)
        beam('Shin',(x,y,.14),(x-.035,y,.45),.14,m['armour'],.17)
        box('KneePad',(.07,.16,.12),(x+.052,y,.43),m['edge'])
        beam('Thigh',(x-.035,y,.46),(-.015,s*.12,.66),.175,m['armour'],.18)
        box('ThighPlate',(.08,.16,.18),(x+.058,y,.56),m['team'])
    facet('Pelvis',(.29,.39,.20),(0,0,.66),m['dark'],taper=.05)
    facet('Torso',(.35,.49,.41),(-.01,0,.89),m['armour'],taper=.17)
    facet('ChestPlate',(.075,.37,.29),(.168,0,.94),m['edge'],taper=.10)
    box('ChestTeam',(.018,.22,.07),(.211,0,1.01),m['team'],bevel=0)
    for y in (-.12,.02,.14): box('MagazinePouch',(.075,.09,.12),(.208,y,.81),m['utility'])
    facet('FieldPack',(.14,.34,.36),(-.235,0,.91),m['armour'],taper=.12)
    box('PackBand',(.145,.28,.065),(-.237,0,.93),m['team'],bevel=0)
    cyl('Neck',.075,.1,(0,0,1.115),m['dark'],verts=8)
    facet('Helmet',(.29,.30,.25),(.015,0,1.265),m['armour'],taper=.19)
    facet('HelmetBrow',(.31,.32,.06),(.035,0,1.335),m['edge'],taper=.1)
    box('Visor',(.025,.24,.077),(.161,0,1.276),m['glass'],bevel=.008)
    facet('Respirator',(.1,.17,.067),(.136,0,1.203),m['dark'],taper=.12)
    for s in (-1,1):
        facet('Shoulder',(.25,.16,.17),(.015,s*.267,1.038),m['team'],taper=.12)
        beam('UpperArm',(.01,s*.277,.99),(.16,s*.295,.88),.12,m['armour'],.13)
        if weapon == 'rocket':
            hand = (.16,.285,1.01) if s>0 else (.44,.285,1.01)
        else:
            hand = (.30,.145,.935) if s>0 else (.49,.145,.935)
        beam('Forearm',(.16,s*.295,.88),hand,.115,m['edge'])
        box('Glove',(.115,.10,.10),hand,m['dark'])
    if weapon=='rocket':
        cyl('Launcher',.076,.88,(.19,.30,1.055),m['armour'],verts=10,rot=(0,90,0))
        for x in (-.24,.61): cyl('LauncherRim',.087,.04,(x,.30,1.055),m['steel'],verts=10,rot=(0,90,0))
        cyl('LauncherBore',.063,.012,(.637,.30,1.055),m['dark'],verts=10,rot=(0,90,0))
        box('LauncherSight',(.15,.07,.08),(.19,.30,1.155),m['glass'])
        box('WarheadCase',(.12,.26,.33),(-.244,0,.98),m['utility'])
    else:
        length=.70 if weapon=='sniper' else .45
        box('RifleReceiver',(.31,.078,.12),(.32,.145,.96),m['steel'])
        box('RifleStock',(.16,.08,.09),(.12,.145,.96),m['dark'])
        cyl('RifleBarrel',.023,length-.2,(.50+(length-.2)/2,.145,.967),m['dark'],verts=8,rot=(0,90,0))
        box('Magazine',(.09,.07,.15),(.33,.145,.865),m['dark'],rot=(0,-12,0))
        box('RifleForegrip',(.15,.082,.05),(.49,.145,.925),m['armour'])
        if weapon=='sniper':
            cyl('Suppressor',.037,.17,(.90,.145,.967),m['steel'],verts=8,rot=(0,90,0))
            cyl('Scope',.037,.21,(.33,.145,1.068),m['dark'],verts=8,rot=(0,90,0))
            cyl('ScopeGlass',.026,.01,(.44,.145,1.068),m['glass'],verts=8,rot=(0,90,0))
            facet('ReconMantle',(.35,.48,.14),(-.025,0,1.065),m['edge'],taper=.1)
        else: box('ReflexSight',(.07,.07,.05),(.33,.145,1.043),m['glass'],bevel=.004)
    join(name); finish(name,3000)


def lancer():
    m=start()
    # Six-wheel long bed, sharply sloping cab; launcher mass stays behind cab.
    loft('LongChassis',[(-1.36,.45,.36,.65),(-1.1,.58,.35,.73),(1.2,.58,.35,.75),(1.45,.48,.43,.68)],m['dark'])
    for x in (-1,0,1):
        for s in (-1,1):
            wheel(x,s*.65,.35,.34,.28,m)
            box('Fender',(.66,.29,.08),(x,s*.65,.72),m['armour'])
    loft('TruckCab',[(.49,.56,.70,1.48),(1.07,.56,.7,1.52),(1.46,.46,.73,1.14)],m['armour'])
    wing('Windscreen',[(1.08,-.42),(1.08,.42),(1.385,.36),(1.385,-.36)],1.40,.03,m['glass'])
    for s in (-1,1):
        box('DoorPanel',(.49,.03,.31),(.83,s*.558,1.00),m['team'])
        box('SideWindow',(.39,.025,.23),(.77,s*.552,1.32),m['glass'])
        lamp(1.44,s*.38,.92,m)
        box('Stabilizer',(.18,.17,.42),(-1.24,s*.56,.61),m['steel'])
    box('Bed',(1.87,1.17,.11),(-.40,0,.77),m['armour'])
    hull=join('Hull'); snap=om.new_parts()
    cyl('TraverseRing',.42,.16,(-.7,0,.91),m['steel'],verts=16)
    facet('LauncherCradle',(.94,.89,.31),(-.70,0,1.07),m['armour'],taper=.12)
    # Build in the existing neutral turret frame, elevate pod geometry alone.
    pod_snap=om.new_parts()
    facet('SixCellPod',(1.45,.88,.55),(-.60,0,1.41),m['edge'],taper=.08)
    for s in (-1,1): box('PodSide',(1.17,.028,.20),(-.6,s*.443,1.42),m['team'])
    for y in (-.28,0,.28):
        for z in (1.28,1.54):
            cyl('LaunchCellRim',.111,.043,(.145,y,z),m['steel'],verts=8,rot=(0,90,0))
            cyl('LaunchCell',.086,.045,(.173,y,z),m['dark'],verts=8,rot=(0,90,0))
            cyl('CellCap',.055,.016,(.2,y,z),m['utility'],verts=8,rot=(0,90,0))
    from mathutils import Matrix
    pivot=Vector((-.6,0,1.41)); rotation=Matrix.Rotation(math.radians(-18),4,'Y')
    for obj in om.parts_since(pod_snap):
        obj.matrix_world=Matrix.Translation(pivot) @ rotation @ Matrix.Translation(-pivot) @ obj.matrix_world
    articulate('Turret',(-.7,0,.85),hull,snap); finish('lancer')


def jammer():
    m=start(); vehicle_base(m,cab_x=.61,cab_length=.80)
    facet('ElectronicsShelter',(1.19,1.0,.67),(-.42,0,1.08),m['armour'],taper=.06)
    for s in (-1,1):
        box('EquipmentPanel',(.80,.025,.28),(-.47,s*.5,1.02),m['team'])
        for x in (-.78,-.63,-.48,-.33,-.18): box('CoolingLouvre',(.05,.025,.13),(x,s*.516,1.29),m['steel'],bevel=0)
        lamp(1.065,s*.35,.79,m)
    box('ShelterRoof',(1.1,.93,.06),(-.42,0,1.441),m['edge'])
    hull=join('Hull'); snap=om.new_parts()
    cyl('EmitterBase',.31,.14,(-.4,0,1.52),m['team'],verts=12)
    cyl('MastLower',.087,.74,(-.4,0,1.98),m['steel'],verts=8)
    cyl('MastUpper',.047,.76,(-.4,0,2.63),m['edge'],verts=8)
    facet('ArrayFrame',(.91,.17,.65),(-.4,0,3.00),m['armour'],taper=.04)
    for x in (-.70,-.4,-.10):
        for z in (2.81,3.0,3.19):
            box('AESAElement',(.24,.018,.145),(x,.093,z),m['glass'],bevel=.008)
    box('ArrayBand',(.70,.182,.045),(-.4,0,3.326),m['team'],bevel=0)
    for s in (-1,1):
        rod('FoldedBrace',(-.4,s*.10,2.60),(-.77,s*.07,2.82),.021,m['steel'])
        cyl('BroadbandAerial',.019,.62,(-.73,s*.36,1.97),m['dark'],verts=6)
    articulate('Turret',(-.4,0,1.45),hull,snap); finish('jammer')


def hive():
    m=start(); tracks(2.38,.735,.35,.42,m,bottom=.09)
    loft('CarrierHull',[(-1.16,.57,.39,.87),(-.8,.64,.37,.92),(.77,.64,.39,.90),(1.18,.53,.44,.75)],m['armour'])
    for s in (-1,1):
        box('Fender',(2.30,.34,.065),(0,s*.735,.61),m['edge'])
        for x in (-.84,-.39,.06,.51,.96):
            box('Skirt',(.38,.055,.22),(x,s*.914,.54),m['team'] if x==.06 else m['armour'])
        lamp(1.145,s*.39,.79,m)
    facet('DroneMagazine',(1.67,1.08,.47),(-.29,0,1.135),m['armour'],taper=.13)
    box('MagazineFrame',(1.49,.92,.055),(-.29,0,1.395),m['dark'])
    # Eight rectangular hatches and a clearly separated launch spine.
    for x in (-.87,-.48,-.09,.30):
        for s in (-1,1):
            facet('CellLid',(.31,.33,.055),(x,s*.235,1.455),m['team'],taper=.02)
            box('CellHinge',(.08,.035,.065),(x,s*.415,1.449),m['steel'],bevel=0)
    box('LaunchRail',(1.51,.075,.1),(-.28,0,1.46),m['steel'])
    loft('ForwardControlCell',[(.54,.51,.89,1.37),(.92,.51,.83,1.38),(1.14,.43,.75,1.04)],m['edge'])
    box('ControlOptics',(.027,.57,.12),(1.07,0,1.13),m['glass'])
    vent((-.93,0,.89),(.34,.85),m)
    join('coalition_hive'); finish('hive')


def drone():
    m=start()
    loft('DroneBody',[(-.295,.045,.185,.23),(-.16,.11,.15,.25),(.13,.095,.155,.235),(.245,.026,.185,.215)],m['armour'])
    for s in (-1,1):
        wing('SweptWing',[(-.16,s*.08),(-.205,s*.445),(-.115,s*.44),(.11,s*.07)],.205,.027,m['team'])
        wing('Tailplane',[(-.29,s*.025),(-.305,s*.15),(-.25,s*.15),(-.19,s*.02)],.247,.022,m['edge'])
    cyl('NoseLens',.029,.022,(.231,0,.197),m['glass'],verts=8,rot=(0,90,0))
    box('DorsalPanel',(.145,.08,.011),(-.04,0,.253),m['team'],bevel=0)
    join('drone'); finish('drone',1800)


def rotor(hull, name, pivot, radius, m, z, blades=4):
    snap=om.new_parts(); x,y,_=pivot
    cyl('RotorHub',.12,.14,(x,y,z-.065),m['steel'],verts=10)
    cyl('HubCap',.072,.035,(x,y,z+.022),m['steel'],verts=8)
    for i in range(blades):
        a=i*2*math.pi/blades
        points=[]
        for xx,yy in ((.08,-.045),(radius*.84,-.075),(radius,-.025),(radius,.065),(.22,.055)):
            points.append((x+xx*math.cos(a)-yy*math.sin(a),y+xx*math.sin(a)+yy*math.cos(a)))
        wing('RotorBlade',points,z,.027,m['dark'])
        for xx in (radius*.84,):
            bx=x+xx*math.cos(a); by=y+xx*math.sin(a)
            box('BladeTip',(.10,.112,.031),(bx,by,z),m['steel'],bevel=0,rot=(0,0,math.degrees(a)))
    return articulate(name,pivot,hull,snap)


def tiltrotor():
    m=start()
    loft('CargoFuselage',[(-1.35,.33,.40,1.19),(-.95,.49,.34,1.32),(.72,.49,.36,1.35),
                          (1.29,.40,.54,1.28),(1.71,.15,.70,1.00)],m['armour'])
    loft('CockpitGlazing',[(.91,.42,1.08,1.34),(1.25,.38,1.02,1.27),(1.54,.24,.94,1.09)],m['glass'])
    for s in (-1,1):
        box('CargoDoor',(.84,.027,.53),(-.38,s*.49,.81),m['edge'])
        box('DoorTeam',(.70,.031,.12),(-.38,s*.508,.94),m['team'])
        box('DoorWindow',(.24,.035,.14),(-.45,s*.511,1.11),m['glass'])
        wing('ShoulderWing',[(.10,s*.34),(.07,s*1.91),(-.48,s*2.03),(-.58,s*.40)],1.32,.12,m['team'])
        facet('GearHousing',(.50,.19,.23),(-.63,s*.47,.34),m['dark'],taper=.08)
        wheel(-.68,s*.48,.29,.087,.07,m)
        wing('Tailplane',[(-1.15,s*.1),(-1.62,s*.65),(-1.99,s*.66),(-1.8,s*.05)],1.29,.07,m['edge'])
    # Split tail fins preserve a clean cargo-aircraft silhouette.
    for s in (-1,1):
        beam('TailFin',(-1.79,s*.52,1.29),(-1.97,s*.52,1.71),.085,m['team'],.055)
    box('CargoRamp',(.31,.72,.18),(-1.23,0,.44),m['steel'],rot=(0,-20,0))
    vent((-.64,0,1.33),(.5,.45),m)
    hull=join('Hull')
    for s in (1,-1):
        rotor(hull,'RotorL' if s>0 else 'RotorR',(-.2,s*2,1.55),1.10,m,1.7)
        snap=om.new_parts()
        facet('NacelleHousing',(.51,.47,.65),(-.2,s*2,1.20),m['armour'],taper=.15)
        cyl('NacelleIntake',.165,.025,(-.2,s*2,1.534),m['dark'],verts=12)
        for xx in (-.29,-.2,-.11): box('NacelleGrille',(.025,.23,.015),(xx,s*2,1.549),m['steel'],bevel=0)
        join('Nacelle' if s>0 else 'Nacelle.001',(0,0,0),snap)
    finish('tiltrotor')


def kestrel():
    m=start()
    loft('AttackFuselage',[(-.92,.18,.72,1.24),(-.40,.34,.54,1.43),(.54,.33,.54,1.43),
                           (1.02,.27,.59,1.26),(1.40,.10,.68,.99)],m['armour'])
    loft('TandemCanopy',[(.02,.25,1.14,1.48),(.46,.28,1.1,1.45),(1.03,.225,.97,1.28),(1.27,.1,.93,1.04)],m['glass'])
    for x in (.18,.61): beam('CanopyFrame',(x,-.275,1.11),(x,.275,1.11),.042,m['edge'])
    box('EngineDeck',(.87,.48,.12),(-.43,0,1.44),m['team'])
    loft('TaperedTail',[(-2.58,.053,1.02,1.25),(-2.15,.08,1.01,1.19),(-.68,.16,.88,1.18)],m['armour'])
    beam('TailFin',(-2.51,0,1.10),(-2.60,0,1.69),.12,m['team'],.06)
    for s in (-1,1):
        wing('WeaponWing',[(.43,s*.24),(.18,s*1.07),(-.24,s*1.04),(-.13,s*.26)],.91,.065,m['edge'])
        cyl('RocketPod',.117,.64,(.27,s*.86,.77),m['armour'],verts=10,rot=(0,90,0))
        for yy,zz in ((-.047,-.038),(.047,-.038),(0,.047)):
            cyl('RocketCell',.025,.018,(.60,s*.86+yy,.77+zz),m['dark'],verts=6,rot=(0,90,0))
        missile((.30,s*.65,.70),.61,.047,m)
        beam('LandingStrut',(-.08,s*.23,.58),(-.15,s*.39,.36),.04,m['steel'])
        beam('LandingStrut',(.74,s*.20,.61),(.83,s*.39,.36),.04,m['steel'])
        box('LandingSkid',(1.39,.052,.05),(.32,s*.39,.35),m['steel'],bevel=0)
        cyl('TurbineExhaust',.104,.28,(-.78,s*.245,1.35),m['dark'],verts=10,rot=(0,90,0))
    hull=join('Hull'); rotor(hull,'RotorMain',(.2,0,1.55),1.70,m,1.68)
    # The existing game spins every Rotor* around Y. Retain this exact tail node
    # and neutral frame; do not silently change the runtime animation contract.
    snap=om.new_parts()
    box('TailBlade',(.06,.58,.045),(-2.55,.16,1.10),m['steel'],bevel=0)
    cyl('TailHub',.065,.035,(-2.55,.16,1.10),m['edge'],verts=8)
    articulate('RotorTail',(-2.55,.16,1.1),hull,snap)
    snap=om.new_parts()
    facet('ChinCradle',(.25,.22,.15),(1.22,0,.575),m['armour'],taper=.1)
    cyl('ChinGun',.034,.48,(1.52,0,.55),m['steel'],verts=8,rot=(0,90,0))
    cyl('GunBore',.046,.045,(1.78,0,.55),m['dark'],verts=8,rot=(0,90,0))
    articulate('Turret',(1.2,0,.55),hull,snap); finish('kestrel')


def vantage():
    m=start()
    loft('FighterFuselage',[(-1.75,.17,.63,.95),(-1.13,.31,.58,1.0),(-.3,.34,.57,1.04),
                           (.69,.25,.62,1.12),(1.28,.035,.78,.85)],m['armour'])
    loft('Canopy',[(.20,.20,.95,1.14),(.56,.20,.96,1.25),(.99,.06,.91,1.02)],m['glass'])
    for s in (-1,1):
        wing('CrankedWing',[(.33,s*.22),(-.22,s*1.33),(-.66,s*1.69),(-1.12,s*1.67),(-.63,s*.29)],.755,.067,m['edge'])
        wing('WingTeam',[(-.32,s*.68),(-.64,s*1.42),(-.93,s*1.39),(-.55,s*.65)],.797,.012,m['team'])
        wing('Tailplane',[(-1.04,s*.13),(-1.52,s*.67),(-1.82,s*.67),(-1.64,s*.13)],.895,.053,m['armour'])
        beam('CantedFin',(-1.48,s*.18,.91),(-1.65,s*.35,1.51),.22,m['team'],.055)
        cyl('Intake',.13,.28,(-.09,s*.31,.70),m['dark'],verts=10,rot=(0,90,0))
        missile((-.16,s*1.03,.60),.76,.06,m)
        box('WeaponPylon',(.23,.045,.13),(-.21,s*1.03,.70),m['steel'])
    cyl('EngineNozzle',.195,.29,(-1.66,0,.795),m['steel'],verts=12,rot=(0,90,0))
    cyl('EngineCore',.145,.018,(-1.82,0,.795),m['dark'],verts=12,rot=(0,90,0))
    marks(-.64,0,1.048,m)
    join('coalition_vantage'); finish('vantage')


def spectre():
    m=start()
    # Continuous cranked flying-wing outline replaces the disconnected slabs.
    outline=[(.70,0),(.17,.59),(-.94,2.98),(-1.68,2.78),(-1.03,1.34),(-1.55,.78),(-1.11,0),
             (-1.55,-.78),(-1.03,-1.34),(-1.68,-2.78),(-.94,-2.98),(.17,-.59)]
    prism('FlyingWing',outline,.70,.89,m['armour'],inset=.035)
    loft('CentralSpine',[(-1.40,.26,.71,.90),(-.58,.44,.66,1.10),(.23,.32,.69,1.11),(.68,.045,.73,.85)],m['edge'])
    for s in (-1,1):
        wing('TeamChevron',[(.02,s*.45),(-.56,s*1.50),(-.77,s*1.48),(-.22,s*.42)],.915,.014,m['team'])
        wing('Elevon',[(-1.11,s*1.48),(-1.57,s*2.70),(-1.29,s*2.80),(-.93,s*1.48)],.906,.025,m['steel'])
        facet('DorsalIntake',(.47,.26,.12),(-.40,s*.36,1.03),m['dark'],taper=.2)
        box('ExhaustSlot',(.035,.42,.07),(-1.26,s*.40,.81),m['dark'],bevel=0)
    wing('Cockpit',[(.36,-.11),(.05,-.19),(-.17,0),(.05,.19),(.36,.11)],1.105,.022,m['glass'])
    join('coalition_spectre'); finish('spectre')


MODELS=dict(dozer=dozer,warden=warden,rifleman=lambda:soldier('rifleman','rifle'),
            rocket_trooper=lambda:soldier('rocket_trooper','rocket'),pathfinder=lambda:soldier('pathfinder','sniper'),
            lancer=lancer,jammer=jammer,hive=hive,drone=drone,tiltrotor=tiltrotor,kestrel=kestrel,vantage=vantage,spectre=spectre)
