"""Distinct civilian conversions and irregular infantry for the Network roster."""
import math
import bpy
import omlib as om
from network_shapes import *

def person(role,m,offset=(0,0,0),scale=1):
    snap=om.new_parts()
    cloth=m['dark'] if role=='saboteur' else m['tarp']
    for side in (-1,1):
        y=side*.13
        box('Boot',(.27,.185,.105),(.035+side*.025,y,.055),m['dark'],.025)
        box('BootSole',(.275,.19,.025),(.035+side*.025,y,.012),m['steel'],.005)
        beam('TrouserShin',(0,y,.14),(-.025,y,.39),.09,cloth)
        beam('TrouserThigh',(-.025,y,.39),(0,side*.11,.66),.10,cloth)
        box('KneePatch',(.05,.135,.135),(.067,y,.40),m['sand'],.025)
    loft('Jacket',[(-.12,.20,.60,.98),(.07,.23,.61,1.04),(.155,.19,.64,.99)],cloth)
    box('Belt',(.30,.44,.06),(.015,0,.65),m['dark'],.012)
    box('Buckle',(.035,.07,.055),(.18,0,.65),m['light'],.005)
    box('VestFront',(.055,.34,.29),(.17,0,.87),m['sand'],.015)
    for y in (-.115,0,.115):box('AmmoPouch',(.075,.085,.145),(.211,y,.79),m['tarp'],.015)
    box('TeamSash',(.06,.12,.36),(.17,-.11,.91),m['team'],.01,rot=(6,0,0))
    for side in (-1,1):
        beam('UpperArm',(0,side*.25,.99),(.19,side*.28,.86),.083,cloth)
        beam('Sleeve',(.19,side*.28,.86),(.35,side*.16,.92),.065,cloth)
        box('Hand',(.105,.09,.085),(.36,side*.16,.92),m['light'],.017)
        box('SleevePatch',(.115,.026,.10),(.10,side*.319,.96),m['team'],.008)
    cyl('Neck',.068,.095,(0,0,1.095),m['light'],8)
    box('Face',(.205,.20,.22),(.015,0,1.205),m['light'],.045)
    box('Nose',(.04,.055,.05),(.126,0,1.20),m['light'],.012)
    if role=='worker':
        cyl('HardHat',.145,.06,(0,0,1.33),m['sand'],12)
        box('Crown',(.22,.23,.09),(0,0,1.34),m['sand'],.04)
        beam('ShovelShaft',(.30,.30,.30),(.30,.30,1.18),.023,m['rust'])
        box('ShovelSpade',(.17,.04,.22),(.30,.30,.22),m['steel'],.02)
        box('ToolApron',(.06,.29,.19),(.18,0,.62),m['rust'],.015)
        box('ApronPocket',(.04,.16,.08),(.215,0,.65),m['sand'],.006)
        box('ToolSatchel',(.10,.21,.22),(-.145,.05,.77),m['sand'],.015)
    else:
        box('WrappedHead',(.26,.27,.10),(-.003,0,1.31),m['tarp'],.035)
        box('Headband',(.265,.275,.035),(.002,0,1.30),m['team'],.008)
        box('ScarfFold',(.08,.23,.095),(.10,0,1.13),m['sand'],.015)
        box('ScarfTail',(.06,.075,.20),(-.105,-.09,1.14),m['sand'],.015,rot=(0,-12,0))
    if role in ('rebel','rpg_trooper'):
        if role=='rebel':
            box('RifleReceiver',(.35,.075,.10),(.35,.15,.94),m['steel'],.009)
            box('WoodStock',(.18,.068,.095),(.09,.15,.93),m['rust'],.015)
            box('Magazine',(.08,.065,.15),(.35,.15,.85),m['dark'],.01,rot=(0,-12,0))
            cyl('RifleBarrel',.019,.27,(.61,.15,.958),m['steel'],8,(0,90,0))
            box('ForeGrip',(.13,.08,.06),(.52,.15,.93),m['rust'],.009)
            box('FrontSight',(.015,.04,.06),(.68,.15,.986),m['dark'],.002)
        else:
            cyl('Launcher',.072,.82,(.16,.30,1.025),m['steel'],12,(0,90,0))
            cyl('LauncherGrip',.083,.26,(.11,.30,1.025),m['tarp'],12,(0,90,0))
            cyl('FrontRim',.096,.06,(.59,.30,1.025),m['rust'],12,(0,90,0))
            cyl('LauncherBore',.066,.005,(.625,.30,1.025),m['dark'],12,(0,90,0))
            box('LauncherSight',(.12,.025,.12),(.28,.25,1.105),m['dark'],.008)
            box('RocketBag',(.17,.27,.34),(-.16,-.025,.87),m['sand'],.02)
    if role=='saboteur':
        box('DemolitionPack',(.23,.36,.36),(-.23,0,.84),m['rust'],.025)
        for y in (-.11,0,.11):cyl('SealedCharge',.045,.28,(-.36,y,.86),m['sand'],8)
        box('Detonator',(.11,.17,.07),(.35,.08,.95),m['steel'],.01)
        box('GoggleStrap',(.23,.24,.055),(.013,0,1.225),m['dark'],.01)
        box('GoggleLens',(.03,.16,.042),(.141,0,1.228),m['glass'],.005)
        beam('PackCord',(-.27,.16,1.02),(.22,.16,.91),.013,m['dark'],6)
    if role=='fpv_operator':
        box('Controller',(.24,.32,.065),(.37,0,.97),m['steel'],.018)
        box('ControllerScreen',(.13,.16,.015),(.39,0,1.012),m['glass'],.004)
        for y in (-.11,.11):cyl('ThumbStick',.017,.035,(.35,y,1.017),m['dark'],8)
        beam('RadioAerial',(.30,.13,1.015),(.30,.13,1.44),.012,m['dark'],6)
        box('FPVGoggles',(.11,.27,.10),(.12,0,1.235),m['dark'],.02)
        box('GogglePlate',(.025,.17,.06),(.18,0,1.24),m['sand'],.006)
        box('RadioPack',(.13,.28,.29),(-.16,0,.87),m['sand'],.022)
        for z in (.78,.88,.98):box('PackVent',(.015,.19,.022),(-.233,0,z),m['dark'],0)
    bpy.context.view_layer.update()
    for ob in om.parts_since(snap):
        ob.location=Vector(offset)+ob.location*scale
        ob.scale*=scale

def infantry(name,m):person(name,m)

def angry_mob(m):
    for i,(x,y) in enumerate(((.04,0),(.59,.32),(-.49,.40),(.46,-.43),(-.58,-.32))):
        person('rebel' if i<2 else 'worker',m,(x,y,0),.9)

def pickup(m,bomb=False):
    length=2.4 if bomb else 2.16
    loft('WeldedChassis',[(-length/2,.47,.38,.58),(length/2,.46,.38,.61)],m['rust'])
    cabx=.85 if bomb else .62
    loft('CabShell',[(cabx-.41,.48,.57,1.30),(cabx+.19,.47,.58,1.32),(cabx+.42,.44,.57,1.06)],m['sand'])
    box('Roof',(.66,1.0,.045),(cabx-.08,0,1.335),m['team'],.02)
    box('Windshield',(.026,.78,.31),(cabx+.325,0,1.20),m['glass'],.008,rot=(0,-42,0))
    box('WindshieldDivider',(.025,.035,.315),(cabx+.342,0,1.21),m['steel'],.003,rot=(0,-42,0))
    for side in (-1,1):
        box('SideWindow',(.40,.025,.27),(cabx-.06,side*.477,1.14),m['glass'],.009)
        panel(cabx-.07,side*.50,.82,.57,.25,m)
        box('DoorTeamPatch',(.24,.015,.18),(cabx-.08,side*.525,.83),m['team'],.008)
        box('DoorHandle',(.1,.027,.02),(cabx-.27,side*.53,.99),m['steel'],.003)
        beam('MirrorStem',(cabx+.17,side*.45,1.18),(cabx+.10,side*.59,1.22),.014,m['steel'])
        box('Mirror',(.05,.055,.10),(cabx+.10,side*.59,1.22),m['dark'],.01)
        box('RearBedRail',(1.04,.06,.25),(-.55,side*.49,.79),m['sand'],.01)
        for x in (-.83,-.38):box('BedBrace',(.04,.077,.27),(x,side*.49,.79),m['rust'],0)
        wheel(.73 if not bomb else .90,side*.555,.31,m)
        wheel(-.73,side*.555,.31,m)
        box('Headlamp',(.035,.12,.075),(length/2+.015,side*.30,.70),m['light'],.01)
    box('Bumper',(.09,1.11,.095),(length/2,0,.49),m['steel'],.017)
    for y in (-.22,-.11,0,.11,.22):box('Grille',(.014,.045,.12),(length/2+.05,y,.66),m['dark'],0)
    box('BedFloor',(1.08,.88,.07),(-.52,0,.65),m['dark'],0)
    box('Tailgate',(.065,.98,.27),(-length/2,0,.78),m['rust'],.015)

def technical(m):
    pickup(m);crate(-.81,.27,.70,.27,m)
    body=hull();snap=om.new_parts()
    cyl('GunPedestal',.085,.34,(-.5,0,1.10),m['steel'],10)
    cyl('SwivelRing',.14,.055,(-.5,0,1.02),m['dark'],12)
    box('Breech',(.36,.15,.14),(-.29,0,1.41),m['steel'],.014)
    cyl('Barrel',.035,.50,(.12,0,1.43),m['dark'],10,(0,90,0))
    cyl('Muzzle',.043,.08,(.40,0,1.43),m['steel'],10,(0,90,0))
    cyl('Bore',.027,.007,(.444,0,1.43),m['dark'],8,(0,90,0))
    box('GunShield',(.04,.50,.24),(-.13,0,1.37),m['rust'],.018)
    box('GunShieldPatch',(.017,.18,.14),(-.10,.14,1.38),m['team'],.005)
    box('AmmoBox',(.23,.16,.17),(-.39,-.19,1.34),m['sand'],.016)
    for x in (-.42,-.36,-.30):beam('AmmoBelt',(x,-.12,1.43),(x,-.23,1.42),.017,m['light'],6)
    turret('technical',body,snap)

def marauder(m):
    loft('SalvagedHull',[(-1.06,.56,.30,.72),(-.75,.58,.31,.84),(.49,.60,.30,.85),(1.03,.49,.29,.57)],m['rust'])
    tracks(2.12,.70,.30,m)
    for side in (-1,1):
        box('WeldedFender',(2.03,.30,.07),(-.015,side*.70,.62),m['steel'],.012)
        for i,x in enumerate((-.80,-.40,.01,.40,.81)):
            box('ScrapSkirt',(.34,.065,.22),(x,side*.83,.57),m['team'] if i==2 else m['sand'] if i%2 else m['rust'],.012,rot=(i%2*6,0,0))
            for xx in (-.12,.12):box('SkirtBolt',(.025,.025,.025),(x+xx,side*.874,.62),m['light'],.003)
        cyl('RearExhaust',.065,.24,(-1.035,side*.43,.72),m['steel'],10,(0,90,0))
    box('EngineGrille',(.45,.73,.025),(-.75,0,.851),m['dark'],.008)
    for x in (-.90,-.82,-.74,-.66,-.58):box('GrilleBar',(.025,.66,.03),(x,0,.878),m['steel'],0)
    box('GlacisPlate',(.53,.82,.035),(.76,0,.74),m['sand'],.01,rot=(0,27,0))
    for side in (-1,1):
        box('TowLug',(.10,.13,.09),(1.035,side*.36,.48),m['steel'],.012)
        for x in (.58,.91):
            cyl('GlacisRivet',.018,.024,(x,side*.31,.865-(x-.52)*.51),m['steel'],6)
    cyl('TurretRace',.42,.12,(-.1,0,.86),m['dark'],16)
    body=hull();snap=om.new_parts()
    loft('WeldedTurret',[(-.60,.34,.90,1.19),(-.38,.46,.90,1.28),(.30,.40,.90,1.24),(.45,.23,.95,1.13)],m['rust'])
    for side in (-1,1):
        box('TurretApplique',(.56,.06,.20),(-.09,side*.414,1.10),m['sand'],.015,rot=(0,5,side*4))
        box('TeamPaint',(.31,.02,.13),(-.04,side*.451,1.13),m['team'],.004)
    box('Mantlet',(.25,.25,.20),(.40,0,1.09),m['steel'],.024)
    cyl('Cannon',.056,.69,(.84,0,1.10),m['steel'],10,(0,90,0))
    cyl('CannonCollar',.079,.16,(.69,0,1.10),m['sand'],10,(0,90,0))
    box('MuzzleBrake',(.16,.12,.11),(1.23,0,1.10),m['dark'],.012)
    cyl('Hatch',.17,.055,(-.26,.11,1.31),m['steel'],12)
    box('HatchHandle',(.105,.04,.045),(-.27,.11,1.355),m['dark'],.008)
    box('Periscope',(.12,.13,.09),(.01,-.14,1.31),m['steel'],.01)
    box('Sight',(.013,.085,.045),(.078,-.14,1.32),m['glass'],0)
    crate(-.45,-.14,1.25,.18,m)
    turret('marauder',body,snap)

def raider_quad(m):
    loft('ATVEngine',[(-.56,.23,.26,.54),(.47,.23,.28,.59)],m['rust'])
    for side in (-1,1):
        for x in (-.45,.45):wheel(x,side*.41,.245,m,.22)
        beam('TubularRail',(-.58,side*.26,.40),(.58,side*.26,.40),.035,m['steel'])
        beam('Shock',(.20,side*.20,.52),(.40,side*.38,.27),.025,m['light'])
        box('FootPlate',(.41,.18,.045),(-.06,side*.27,.36),m['dark'],.006)
    box('RearRack',(.35,.55,.045),(-.48,0,.68),m['steel'],.012)
    box('ATVBonnet',(.37,.49,.12),(.41,0,.65),m['team'],.025)
    box('Seat',(.45,.30,.13),(-.17,0,.71),m['dark'],.025)
    loft('RiderJacket',[(-.23,.13,.80,1.23),(-.02,.18,.81,1.19),(.09,.13,.81,1.17)],m['tarp'])
    box('RiderVest',(.07,.27,.27),(.10,0,1.06),m['sand'],.014)
    box('RiderScarf',(.19,.23,.055),(-.07,0,1.27),m['team'],.012)
    box('RiderHead',(.20,.21,.21),(-.08,0,1.39),m['light'],.045)
    box('DustGoggles',(.055,.18,.055),(.033,0,1.40),m['dark'],.012)
    box('Helmet',(.245,.25,.10),(-.09,0,1.50),m['tarp'],.04)
    for side in (-1,1):
        beam('RiderArm',(-.02,side*.17,1.19),(.39,side*.25,.91),.068,m['tarp'])
        beam('RiderLeg',(-.18,side*.16,.80),(.10,side*.24,.45),.072,m['tarp'])
    beam('Handlebar',(.42,-.29,.89),(.42,.29,.89),.023,m['steel'])
    cyl('GunBarrel',.024,.55,(.75,.19,.83),m['dark'],8,(0,90,0))
    box('GunAction',(.20,.12,.13),(.42,.19,.81),m['steel'],.01)
    cyl('Headlamp',.072,.05,(.59,0,.65),m['light'],12,(0,90,0))

def rocket_buggy(m):
    loft('BuggyFloor',[(-.88,.36,.38,.53),(.80,.34,.38,.58)],m['rust'])
    for side in (-1,1):
        for x in (-.65,.65):wheel(x,side*.54,.29,m)
        beam('CageFront',(.69,side*.34,.51),(.49,side*.33,1.11),.035,m['steel'])
        beam('CageRear',(.0,side*.34,.51),(.04,side*.33,1.11),.035,m['steel'])
        beam('CageRoof',(.04,side*.33,1.11),(.49,side*.33,1.11),.035,m['steel'])
        panel(.31,side*.37,.62,.53,.18,m)
    beam('RoofCross',(.48,-.33,1.11),(.48,.33,1.11),.03,m['steel'])
    box('Seat',(.30,.35,.34),(.17,0,.72),m['dark'],.028)
    box('CanvasRoof',(.54,.70,.035),(.27,0,1.14),m['team'],.012)
    box('Bonnet',(.26,.66,.10),(.68,0,.61),m['sand'],.016)
    cyl('LaunchBearing',.24,.16,(-.5,0,.70),m['steel'],12)
    body=hull();snap=om.new_parts()
    box('RocketRack',(.67,.72,.33),(-.49,0,1.015),m['sand'],.025,rot=(0,-18,0))
    for y in (-.23,0,.23):
        for z in (.94,1.16):
            cyl('LaunchTube',.086,.68,(-.45,y,z),m['steel'],10,(0,72,0))
            cyl('TubeRim',.093,.045,(-.117,y,z+.109),m['rust'],10,(0,72,0))
            cyl('TubeBore',.065,.046,(-.105,y,z+.113),m['dark'],10,(0,72,0))
    for side in (-1,1):box('RackTeamPanel',(.40,.025,.20),(-.50,side*.397,1.06),m['team'],.008,rot=(0,-18,0))
    turret('rocket_buggy',body,snap)

def bomb_truck(m):
    pickup(m,True)
    box('CargoBox',(1.35,.96,.57),(-.42,0,1.05),m['sand'],.025)
    for x in (-.96,-.53,-.08):
        box('CargoBrace',(.04,.99,.59),(x,0,1.05),m['rust'],0)
    canvas((-.44,0,1.57),(1.43,1.10),m)
    for x in (-.94,-.25):
        for side in (-1,1):beam('CargoTie',(x,side*.50,1.55),(x+.12,side*.55,.77),.014,m['light'],6)
    for x in (-.9,-.45):drum(x,.66,.64,.14,m)
    crate(-.9,-.26,.80,.31,m)

def radar_van(m):
    loft('VanShell',[(-1.07,.47,.39,1.31),(.71,.48,.38,1.31),(1.08,.42,.40,1.12)],m['sand'])
    box('Windscreen',(.03,.75,.34),(.93,0,1.22),m['glass'],.008,rot=(0,-63,0))
    box('FrontBumper',(.075,1.03,.085),(1.09,0,.46),m['steel'],.013)
    for side in (-1,1):
        box('VanHeadlamp',(.03,.145,.095),(1.104,side*.31,.78),m['light'],.01)
    box('VanGrille',(.026,.41,.16),(1.109,0,.68),m['dark'],.006)
    for y in (-.15,-.05,.05,.15):box('GrilleBar',(.018,.018,.13),(1.131,y,.68),m['steel'],0)
    for side in (-1,1):
        for x in (-.72,.70):wheel(x,side*.58,.30,m)
        box('SideStripe',(1.86,.028,.15),(-.10,side*.48,.94),m['team'],.005)
        panel(-.50,side*.50,.74,.63,.36,m)
        box('CabWindow',(.35,.025,.24),(.53,side*.48,1.15),m['glass'],.01)
        for x in (-.8,-.68,-.56,-.44):box('ElectronicsVent',(.028,.025,.16),(x,side*.53,.76),m['dark'],0)
        beam('RoofRack',(-.97,side*.40,1.39),(.55,side*.40,1.39),.027,m['steel'])
    box('RoofGenerator',(.37,.65,.22),(.44,0,1.45),m['rust'],.024)
    cyl('MastBase',.15,.17,(-.39,0,1.43),m['steel'],12)
    for z,r,d in ((1.73,.068,.45),(2.18,.046,.48),(2.68,.027,.53)):cyl('TelescopicMast',r,d,(-.40,0,z),m['steel'],10)
    for y in (-.38,.38):beam('MastBrace',(-.4,y,1.4),(-.4,0,2.13),.016,m['dark'],6)
    box('RadarBack',(.065,1.1,.42),(-.43,0,2.89),m['rust'],.027)
    box('RadarArray',(.025,.98,.34),(-.382,0,2.89),m['dark'],.012)
    for y in (-.41,-.25,-.09,.09,.25,.41):box('RadarDipole',(.06,.027,.27),(-.352,y,2.89),m['steel'],0)
    box('ArrayPatch',(.032,.22,.13),(-.343,.34,3.03),m['team'],.004)

def sprayer(m):
    loft('IndustrialChassis',[(-1.01,.47,.29,.64),(.71,.45,.30,.69)],m['rust'])
    tracks(1.79,.65,.28,m,-.11)
    loft('Cab',[(.22,.40,.65,1.48),(.62,.39,.65,1.48),(.78,.32,.65,1.18)],m['sand'])
    box('CabRoof',(.56,.85,.06),(.47,0,1.50),m['team'],.017)
    box('ShieldedGlass',(.03,.61,.30),(.727,0,1.31),m['glass'],.008,rot=(0,-28,0))
    cyl('ChemicalTank',.40,.93,(-.58,0,1.23),m['tarp'],16,(0,90,0))
    for x in (-.90,-.28):
        box('TankCradle',(.13,.64,.24),(x,0,.75),m['steel'],.018)
        box('CradleFoot',(.23,.76,.045),(x,0,.635),m['rust'],.008)
    for x in (-.91,-.26):cyl('TankBand',.412,.05,(x,0,1.23),m['steel'],16,(0,90,0))
    cyl('FillerNeck',.095,.12,(-.58,0,1.67),m['rust'],10)
    cyl('FillerLid',.13,.025,(-.58,0,1.74),m['steel'],12)
    box('TankWarning',(.48,.016,.21),(-.58,-.411,1.23),m['light'],.008)
    box('WarningBar',(.29,.017,.065),(-.58,-.422,1.23),m['dark'],0,rot=(0,12,0))
    beam('SprayBoom',(-1.03,-1.15,.72),(-1.03,1.15,.72),.035,m['steel'])
    for y in (-1,-.5,0,.5,1):
        cyl('SprayNozzle',.035,.17,(-1.03,y,.59),m['dark'],8)
        beam('BoomBrace',(-.80,y*.3,.86),(-1.03,y,.72),.015,m['rust'],6)
    beam('FeedPipe',(-.94,0,1.25),(-1.03,0,.72),.035,m['dark'])

def fpv_drone(m):
    box('CarbonFrame',(.28,.22,.025),(0,0,.185),m['dark'],.009)
    box('Battery',(.17,.11,.06),(-.025,0,.218),m['sand'],.012)
    box('BatteryStrap',(.055,.135,.015),(-.025,0,.251),m['team'],.004)
    box('Payload',(.16,.13,.06),(0,0,.09),m['rust'],.012)
    for x,y in ((.25,.25),(-.25,.25),(.25,-.25),(-.25,-.25)):
        beam('CarbonArm',(0,0,.17),(x,y,.19),.022,m['steel'])
        cyl('Motor',.041,.06,(x,y,.214),m['steel'],12)
        cyl('MotorBand',.043,.012,(x,y,.227),m['team'],12)
        box('Propeller',(.265,.027,.009),(x,y,.26),m['dark'],.003,rot=(0,0,27 if x*y>0 else -27))
        cyl('RotorNut',.018,.018,(x,y,.264),m['light'],8)
    box('Camera',(.052,.065,.05),(.14,0,.18),m['dark'],.007)
    cyl('CameraLens',.019,.013,(.174,0,.18),m['glass'],12,(0,90,0))
    for y in (-.09,.09):beam('Skid',(-.12,y,.064),(.12,y,.064),.01,m['steel'],6)

def ied(m):
    # Discreet buried-device silhouette: intentionally no TeamColour surface.
    cyl('EarthenCover',.31,.043,(0,0,.024),m['sand'],14)
    for i in range(12):
        a=i*math.tau/12
        box('LooseStone',(.09,.06,.037),(.235*math.cos(a),.235*math.sin(a),.058),m['rust'] if i%3==0 else m['sand'],.016,rot=(0,0,i*31))
    box('BuriedPlate',(.30,.24,.025),(0,0,.057),m['steel'],.014,rot=(0,0,13))
    box('EarthPatch',(.20,.15,.018),(-.025,-.035,.077),m['sand'],.02,rot=(0,0,-8))
    beam('VisibleWire',(.03,.09,.078),(.29,.23,.09),.011,m['dark'],6)
    beam('WireBend',(.29,.23,.09),(.31,.14,.096),.011,m['dark'],6)

UNITS={'worker':lambda m:infantry('worker',m),'rebel':lambda m:infantry('rebel',m),
 'rpg_trooper':lambda m:infantry('rpg_trooper',m),'saboteur':lambda m:infantry('saboteur',m),
 'fpv_operator':lambda m:infantry('fpv_operator',m),'angry_mob':angry_mob,'technical':technical,
 'raider_quad':raider_quad,'marauder':marauder,'sprayer':sprayer,'rocket_buggy':rocket_buggy,
 'bomb_truck':bomb_truck,'radar_van':radar_van,'fpv_drone':fpv_drone,'ied':ied}
