"""Network buildings: fortified civilian spaces and improvised workshops."""
import math
import omlib as om
from network_shapes import *

def house(m,center=(0,0),size=(2.1,2),height=1.25):
    x,y=center;w,d=size
    box('Masonry',(w,d,height),(x,y,height/2),m['sand'],.04)
    box('Foundation',(w+.06,d+.06,.10),(x,y,.05),m['steel'],.012)
    for side in (-1,1):
        for i in range(3):
            xx=x-w*.36+i*w*.34
            box('MasonryRepair',(.25,.035,.10),(xx,y+side*(d/2+.019),.23+(i%2)*.17),m['rust'],.008)
        box('TeamBand',(w*.64,.032,.14),(x,y+side*(d/2+.024),height*.70),m['team'],.006)
    # Rear service windows and welded repair panels remain readable from either
    # RTS camera approach, while the main entrance stays on the south frontage.
    for xx in (x-w*.30,x+w*.30):
        box('RearServiceWindow',(.24,.026,.18),(xx,y+d/2+.027,height*.43),m['dark'],.008)
        for dx in (-.07,.07):box('RearWindowBar',(.017,.036,.18),(xx+dx,y+d/2+.048,height*.43),m['steel'],0)
    box('RearServicePanel',(.31,.035,.20),(x,y+d/2+.028,height*.29),m['rust'],.012)
    corrugated((x,y,height+.04),(w+.14,d+.14),m)
    door(x,y-d/2-.025,0,w*.31,height*.71,m)
    for xx in (x-w*.33,x+w*.33):
        box('WindowRecess',(.29,.028,.28),(xx,y-d/2-.023,height*.72),m['dark'],.01)
        for dx in (-.095,0,.095):box('WindowBar',(.016,.035,.27),(xx+dx,y-d/2-.043,height*.72),m['steel'],0)

def antenna(x,y,z,height,m):
    cyl('AntennaMast',.035,height,(x,y,z+height/2),m['steel'],8)
    for zz in (height*.38,height*.76):beam('AntennaDipole',(x-.20,y,z+zz),(x+.20,y,z+zz),.012,m['steel'],6)

def command_cell(m):
    sandbags(4.8,4.8,m)
    house(m,(.20,-.15),(2.75,2.55),1.52)
    house(m,(-1.45,1.32),(1.20,1.35),.94)
    canvas((-1.43,1.30,1.08),(1.51,1.55),m)
    box('RoofObservation',(.75,.87,.15),(.50,.10,1.71),m['sand'],.025)
    cyl('SatellitePedestal',.085,.25,(.92,.79,1.82),m['steel'],10)
    cyl('SatelliteDish',.40,.035,(.92,.79,2.09),m['light'],16,(0,-24,0))
    beam('DishFeed',(.70,.79,2.17),(1.02,.79,2.33),.025,m['steel'])
    antenna(1.19,-1.23,1.56,2.24,m)
    for x,y in ((.55,-.95),(1.29,-.55)):beam('MastGuy',(1.19,-1.23,3.02),(x,y,1.58),.012,m['dark'],6)
    box('SignalBanner',(.57,.028,.32),(.89,-1.23,3.48),m['team'],.008)
    crate(1.56,.18,0,.48,m);drum(1.64,1.14,0,.22,m)
    for yy in (-.7,-.4):box('GeneratorVent',(.03,.16,.26),(1.62,yy,.54),m['dark'],.002)
    box('CableRun',(.035,1.25,.035),(1.65,-.20,1.23),m['steel'],0)

def safehouse(m):
    house(m,(0,.23),(2.15,1.97),1.27)
    canvas((.05,-1.18,1.11),(2.2,.95),m)
    for x in (-.97,.97):
        beam('AwningPost',(x,-1.61,0),(x,-1.61,1.14),.033,m['rust'])
        beam('AwningBrace',(x,-1.61,.85),(x,-1.30,1.12),.025,m['steel'])
    sandbags(1.66,.45,m,1)
    crate(-.81,-1.26,0,.35,m)
    box('RoofPatch',(.67,.53,.03),(.48,.55,1.36),m['tarp'],.006,rot=(0,0,11))
    box('RoofWaterTray',(.54,.47,.07),(-.63,.66,1.37),m['steel'],.025)
    for x in (-.69,-.50):box('FrontStep',(.34,.22,.08),(x,-.83,.04),m['steel'],.01)

def supply_stash(m):
    sandbags(3.8,3.8,m)
    house(m,(-.68,.10),(1.92,2.64),1.18)
    box('LoadingDoor',(.055,1.22,.86),(.31,.03,.44),m['dark'],.008)
    for yy in (-.43,-.23,-.03,.17,.37,.57):box('ShutterRib',(.04,.03,.82),(.348,yy,.45),m['steel'],0)
    for x,y,w in ((.92,-1.20,.51),(1.23,-.61,.46),(.72,.42,.49),(1.39,1.11,.46)):crate(x,y,0,w,m)
    crate(.96,-1.18,.36,.37,m)
    canvas((1.04,.53,.86),(1.46,1.65),m)
    for x in (.42,1.66):
        for y in (-.13,1.22):beam('StoragePost',(x,y,0),(x,y,.89),.026,m['rust'])
    drum(1.48,-.96,0,.18,m)
    box('Pallet',(1.20,.52,.07),(1.0,-1.31,.035),m['steel'],.004)

def arms_dealer(m):
    sandbags(3.8,3.8,m)
    house(m,(0,.30),(2.92,2.50),1.48)
    door(0,-.97,.02,1.72,1.22,m)
    box('GarageLintel',(2.02,.12,.15),(0,-1.01,1.36),m['rust'],.017)
    for x in (-.94,.94):box('GaragePost',(.12,.13,1.23),(x,-1.01,.65),m['steel'],.01)
    box('ServiceRamp',(1.49,.65,.08),(0,-1.35,.04),m['steel'],.01)
    for x in (-.54,.54):box('RampGrip',(.10,.64,.02),(x,-1.35,.092),m['dark'],0)
    for x in (-1.64,1.64):
        beam('CraneUpright',(x,-1.35,0),(x,-1.35,1.83),.065,m['rust'])
        beam('CraneBrace',(x,-1.72,.05),(x,-1.35,.77),.04,m['steel'])
    beam('CraneBeam',(-1.7,-1.35,1.86),(1.7,-1.35,1.86),.065,m['steel'])
    cyl('CranePulley',.12,.09,(.92,-1.35,1.83),m['dark'],12,(90,0,0))
    beam('HoistCable',(.92,-1.35,1.75),(.92,-1.35,1.20),.012,m['steel'],6)
    beam('HoistHook',(.92,-1.35,1.20),(1.01,-1.35,1.12),.024,m['steel'])
    drum(-1.57,1.39,0,.20,m);crate(1.46,1.36,0,.44,m)

def tunnel_network(m):
    sandbags(1.78,1.78,m,3)
    cyl('TunnelCollar',.57,.14,(0,0,.48),m['steel'],16)
    cyl('TunnelDark',.48,.015,(0,0,.56),m['dark'],16)
    for x in (-.37,.37):beam('EntryRail',(x,-.35,.36),(x,.30,.83),.035,m['rust'])
    for y in (-.26,-.06,.14):box('TunnelStep',(.67,.07,.035),(0,y,.39+(y+.26)*.45),m['steel'],.004)
    box('HatchLid',(.50,.53,.065),(-.39,.25,.65),m['rust'],.014,rot=(0,-32,0))
    box('TunnelPatch',(.27,.035,.17),(0,-.899,.34),m['team'],.008)
    beam('GunCrossbeam',(-.69,.10,.88),(.69,.10,.88),.055,m['steel'])
    body=hull();snap=om.new_parts()
    box('GunReceiver',(.31,.20,.16),(.03,0,1.04),m['steel'],.014)
    cyl('GunBarrel',.035,.51,(.40,0,1.08),m['dark'],10,(0,90,0))
    box('GunShield',(.045,.49,.25),(.17,0,1.01),m['rust'],.015)
    box('ShieldTeam',(.015,.15,.11),(.202,-.13,1.03),m['team'],.004)
    box('AmmoTin',(.19,.19,.16),(-.16,-.21,1.01),m['sand'],.01)
    turret('tunnel_network',body,snap)

def black_market(m):
    house(m,(0,.24),(2.15,1.97),1.00)
    canvas((0,-1.20,1.17),(2.53,.99),m)
    for x in (-1.14,1.14):beam('AwningPole',(x,-1.61,0),(x,-1.61,1.16),.026,m['rust'])
    box('TeamAwningEdge',(2.44,.03,.16),(0,-1.675,1.09),m['team'],.006)
    box('Counter',(1.95,.40,.55),(0,-1.05,.29),m['sand'],.018)
    box('CounterTop',(2.06,.49,.07),(0,-1.05,.60),m['rust'],.013)
    for x in (-.68,-.17,.39):crate(x,-1.0,.64,.26,m)
    for x in (-.75,-.31,.13,.57):box('CounterRib',(.06,.025,.40),(x,-1.263,.28),m['steel'],0)
    box('TradeSign',(.50,.04,.27),(.64,-.79,1.13),m['light'],.016)
    for x in (.48,.60,.72):box('SignMark',(.042,.02,.15),(x,-.821,1.13),m['dark'],0,rot=(0,0,10))
    cyl('DishStand',.06,.28,(-.66,.60,1.21),m['steel'],8)
    cyl('MarketDish',.33,.045,(-.66,.60,1.43),m['rust'],16,(25,0,0))
    beam('DishFeed',(-.66,.6,1.47),(-.66,.43,1.62),.021,m['steel'])

def compound(m):
    house(m,(0,.17),(2.36,2.22),1.51)
    for side in (-1,1):
        box('OuterWall',(3.35,.18,.83),(0,side*1.61,.42),m['sand'],.02)
        box('WallCap',(3.43,.24,.08),(0,side*1.61,.88),m['light'],.01)
        box('SideWall',(.18,2.48,.83),(side*1.61,.27,.42),m['sand'],.02)
        for x in (-1.50,1.50):
            box('CornerTower',(.43,.43,1.32),(x,side*1.49,.66),m['sand'],.03)
            box('TowerCap',(.49,.49,.09),(x,side*1.49,1.36),m['steel'],.015)
            box('TowerSlit',(.25,.016,.095),(x,side*1.715,1.07),m['dark'],.006)
    box('Gate',(.81,.035,.83),(0,-1.715,.43),m['rust'],.016)
    for x in (-.30,-.15,0,.15,.30):box('GateBar',(.035,.04,.75),(x,-1.746,.43),m['steel'],0)
    box('RoofHQMark',(.86,.79,.018),(.1,.22,1.622),m['team'],.008)
    box('RoofVent',(.49,.44,.11),(-.61,.64,1.64),m['steel'],.018)
    for x in (-.76,-.65,-.54):box('VentSlot',(.037,.34,.015),(x,.64,1.705),m['dark'],0)

def stinger_site(m):
    sandbags(1.85,1.85,m,3)
    cyl('WeaponPlatform',.48,.13,(0,0,.40),m['steel'],14)
    # Poles meet the actual stretched-canvas corners; the roof and gun envelope
    # stay unchanged, and all supports remain on the static Hull.
    for x in (-.905,.685):
        for y in (-.52,.86):beam('NetPole',(x,y,0),(x,y,1.43),.018,m['rust'])
    canvas((-.11,.17,1.43),(1.59,1.38),m)
    box('SiteTeamPatch',(.38,.025,.18),(.24,-.938,.37),m['team'],.006)
    crate(-.55,.54,.31,.27,m)
    body=hull();snap=om.new_parts()
    cyl('LauncherPivot',.15,.25,(0,0,.63),m['steel'],12)
    box('MountCradle',(.41,.38,.20),(0,0,.81),m['rust'],.018)
    for y in (-.145,.145):
        cyl('MissileTube',.077,.85,(.25,y,.98),m['tarp'],12,(0,70,0))
        cyl('TubeFrontRim',.089,.045,(.657,y,1.128),m['steel'],12,(0,70,0))
        cyl('DarkTubeMouth',.065,.049,(.667,y,1.132),m['dark'],12,(0,70,0))
        cyl('TubeBackCap',.084,.04,(-.159,y,.832),m['sand'],12,(0,70,0))
    box('OpticalSight',(.18,.12,.12),(.17,0,1.13),m['steel'],.012)
    box('SightLens',(.02,.075,.06),(.276,0,1.14),m['glass'],.003)
    turret('stinger_site',body,snap)

def drone_workshop(m):
    house(m,(0,.24),(2.14,1.95),1.10)
    box('OpenWorkshop',(1.45,.045,.78),(0,-.76,.43),m['dark'],.01)
    box('Bench',(1.68,.53,.10),(0,-1.05,.59),m['steel'],.013)
    for x in (-.68,.68):box('BenchLeg',(.07,.38,.54),(x,-1.04,.27),m['rust'],.01)
    for x in (-.50,0,.50):
        box('BenchDrone',(.21,.14,.035),(x,-1.05,.67),m['sand'],.006)
        for a in (-1,1):
            beam('DroneArm',(x-.15,-1.05+a*.12,.68),(x+.15,-1.05-a*.12,.68),.015,m['dark'],6)
            for dx in (-.14,.14):cyl('PropDisc',.055,.008,(x+dx,-1.05+a*.12,.70),m['steel'],8)
    box('PartsBoard',(1.34,.05,.28),(0,-.798,.92),m['sand'],.012)
    for x in (-.51,-.25,0,.25,.51):box('HangingSpare',(.10,.06,.16),(x,-.841,.92),m['steel'],.006)
    box('SolarPanel',(1.20,.82,.035),(-.27,.35,1.22),m['glass'],.015,rot=(0,8,0))
    for x in (-.72,-.45,-.18,.09):box('SolarCellLine',(.012,.77,.025),(x,.35,1.251-(x+.27)*.14),m['steel'],0)
    antenna(.82,.92,1.12,1.57,m)
    beam('AntennaBrace',(.82,.92,2.20),(.35,.85,1.17),.012,m['dark'],6)
    crate(-.86,-.98,0,.33,m)

def launch_site(m):
    sandbags(3.80,3.80,m)
    box('LaunchPad',(2.48,2.45,.14),(.10,.08,.07),m['steel'],.035)
    for y in (-.72,.72):
        box('LaunchRail',(2.26,.16,.13),(.09,y,.24),m['rust'],.02)
        for x in (-.90,.95):box('Outrigger',(.31,.41,.12),(x,y,.15),m['sand'],.013)
    box('LaunchCradle',(1.44,1.43,.37),(.07,.03,.47),m['dark'],.025,rot=(0,-18,0))
    for y in (-.46,0,.46):
        for z in (.52,.88,1.22):
            cyl('LaunchTube',.115,1.42,(.21,y,z),m['steel'],12,(0,70,0))
            cyl('TubeCollar',.135,.075,(.87,y,z+.241),m['rust'],12,(0,70,0))
            cyl('TubeMouth',.095,.078,(.884,y,z+.246),m['dark'],12,(0,70,0))
    for side in (-1,1):box('LaunchShield',(1.31,.075,.30),(.04,side*.69,.73),m['team'],.012,rot=(0,-18,0))
    house(m,(-1.18,-1.13),(.86,.91),.82)
    box('ControlPanel',(.04,.35,.19),(-.727,-1.15,.58),m['dark'],.007)
    box('ControlScreen',(.017,.20,.095),(-.70,-1.16,.61),m['glass'],.002)
    crate(1.20,-1.20,0,.40,m)

BUILDINGS={n:globals()[n] for n in ('command_cell','safehouse','supply_stash','arms_dealer','tunnel_network','black_market','compound','stinger_site','drone_workshop','launch_site')}
