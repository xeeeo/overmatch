"""Detailed Coalition base architecture, sharing the approved vehicle palette.

Footprints and gameplay roles are unchanged. Major roof profiles, stepped
armoured masses, inset access bays and readable functional hardware provide
silhouette differences before small details are added.
"""
import math
from mathutils import Matrix
import bpy
import omlib as om
from coalition_art import *


def slab(w,d,m,height=.18):
    facet('Foundation',(w-.3,d-.3,height),(0,0,height/2),m['steel'],cut=.05,taper=.015)
    for s in (-1,1):
        box('FoundationInset',(w-.65,.025,.04),(0,s*((d-.3)/2-.02),height*.52),m['dark'],bevel=0)


def ring(name, x,y,z,r,width,mat,segments=32):
    vertices=[]
    for radius in (r,r-width):
        vertices += [(x+radius*math.cos(i*math.tau/segments), y+radius*math.sin(i*math.tau/segments),z) for i in range(segments)]
    faces=[(i,(i+1)%segments,(i+1)%segments+segments,i+segments) for i in range(segments)]
    return mesh(name,vertices,faces,mat)


def panel_door(x,y,z,w,h,m):
    # On a +X facade, inset shutters behind a structural frame.
    box('AccessRecess',(.06,w,h),(x,y,z),m['dark'],bevel=.008)
    for i in range(6):
        box('DoorSlat',(.069,w-.09,h*.105),(x+.012,y,z-h*.42+i*h*.166),m['steel'],bevel=0)
    for s in (-1,1): box('DoorJamb',(.11,.09,h+.10),(x+.026,y+s*(w/2+.015),z),m['edge'])
    box('DoorLintel',(.12,w+.15,.11),(x+.03,y,z+h/2+.04),m['team'])
    box('DoorControl',(.06,.11,.16),(x+.07,y-w*.63,z+.07),m['glass'])


def window_band(x,y,z,w,h,m):
    box('WindowBand',(.027,w,h),(x,y,z),m['glass'],bevel=0)
    for yy in (-w*.36,0,w*.36): box('WindowMullion',(.035,.045,h+.025),(x+.02,y+yy,z),m['edge'],bevel=0)


def crate(x,y,z,size,m):
    facet('CargoCrate',(size,size,size),(x,y,z+size/2),m['utility'],cut=.08,taper=.02)
    for yy in (-.28,.28): box('CrateStrap',(size*.92,.055,size+.012),(x,y+size*yy,z+size/2),m['steel'],bevel=0)
    box('CrateLabel',(.08,size*.43,size*.15),(x+size/2,y,z+size*.6),m['marking'],bevel=0)


def cooling_unit(x,y,z,w,d,m):
    facet('CoolingUnit',(w,d,.30),(x,y,z+.15),m['edge'],taper=.10)
    for xx in (-w*.22,w*.22):
        cyl('CoolingFanWell',min(w*.19,d*.37),.023,(x+xx,y,z+.314),m['dark'],verts=12)
        for a in (0,60,120): box('FanBlade',(w*.27,.035,.023),(x+xx,y,z+.331),m['steel'],bevel=0,rot=(0,0,a))


def mast(x,y,z,height,m,panel=.35):
    cyl('Mast',.04,height,(x,y,z+height/2),m['steel'],verts=8)
    facet('AntennaPanel',(.11,panel,panel*.9),(x,y,z+height),m['edge'],taper=.1)
    box('AntennaFace',(.015,panel*.78,panel*.67),(x+.063,y,z+height),m['glass'],bevel=0)


def roof_gable(name, x,y,z,w,d,rise,m):
    vertices=[(x-w/2,y-d/2,z),(x+w/2,y-d/2,z),(x+w/2,y+d/2,z),(x-w/2,y+d/2,z),
              (x,y-d/2,z+rise),(x,y+d/2,z+rise)]
    return mesh(name,vertices,[(0,1,4),(3,5,2),(0,4,5,3),(4,1,2,5),(0,3,2,1)],m)


def command_post():
    m=start(); slab(5,5,m)
    facet('ArmouredCommandBlock',(3.45,2.85,1.31),(.25,.1,.835),m['armour'],cut=.12,taper=.07)
    facet('RoofParapet',(3.35,2.73,.19),(.20,.1,1.55),m['edge'],cut=.11,taper=.025)
    facet('RaisedOperations',(1.95,1.45,.46),(.35,.15,1.83),m['armour'],taper=.12)
    box('OperationsRoof',(1.68,1.14,.075),(.31,.15,2.091),m['team'])
    for s in (-1,1):
        for y in (-.90,0,.90):
            facet('BlastButtress',(.29,.22,1.08),(1.84 if s>0 else -1.39,y,.74),m['edge'],cut=.08,taper=.23)
    panel_door(1.955,-.57,.76,.85,1.01,m)
    window_band(1.84,.68,1.25,.58,.18,m)
    facet('CommsWing',(1.22,1.22,.94),(-1.63,1.19,.65),m['edge'],taper=.10)
    cooling_unit(-1.63,1.18,1.12,.87,.70,m)
    # Forward control tower has an armoured tapered base and cantilevered cap.
    facet('TowerShaft',(.84,.88,2.04),(-1.5,-1.11,1.2),m['armour'],taper=.13)
    facet('TowerGlass',(1.04,1.07,.42),(-1.5,-1.11,2.39),m['glass'],taper=.10)
    facet('TowerCap',(1.12,1.15,.14),(-1.5,-1.11,2.69),m['team'],taper=.08)
    for yy in (-1.56,-.66): beam('TowerPillar',(-1.01,yy,2.16),(-1.01,yy,2.61),.065,m['edge'])
    cyl('RadarPedestal',.24,.32,(1.04,.92,1.91),m['steel'],verts=12)
    beam('RadarYoke',(.91,.3,2.12),(.91,1.54,2.12),.095,m['steel'])
    facet('RadarArray',(.15,1.52,.61),(1.04,.92,2.68),m['armour'],taper=.05)
    for yy in (.39,.74,1.09,1.44): box('RadarElement',(.018,.26,.44),(1.125,yy,2.68),m['glass'],bevel=.004)
    box('RadarTop',(.18,1.4,.044),(1.04,.92,3.02),m['team'],bevel=0)
    vent((.2,-.86,1.68),(.94,.35),m)
    join('command_post'); finish('command_post',10000)


def power_plant():
    m=start(); slab(3,3,m)
    facet('GeneratorHall',(1.80,2.12,1.1),(-.35,0,.75),m['armour'],taper=.06)
    roof_gable('GeneratorRoof',-.35,0,1.32,1.91,2.19,.26,m['edge'])
    box('RoofIdentification',(.40,1.7,.036),(-.35,0,1.589),m['team'])
    for y in (-.64,.64):
        cyl('TurbineDrum',.28,1.61,(1.01,y,.59),m['steel'],verts=16,rot=(0,90,0))
        for x in (.39,.85,1.31,1.79): cyl('DrumBand',.295,.052,(x,y,.59),m['edge'],verts=16,rot=(0,90,0))
        cyl('TurbineIntake',.22,.02,(1.85,y,.59),m['dark'],verts=14,rot=(0,90,0))
        for zz in (.49,.59,.69): box('IntakeGrille',(.025,.37,.026),(1.872,y,zz),m['steel'],bevel=0)
        cyl('ExhaustStack',.145,1.06,(1.13,y,1.37),m['dark'],verts=12)
        cyl('StackShield',.20,.37,(1.13,y,1.61),m['steel'],verts=12)
        cyl('StackLip',.215,.065,(1.13,y,1.91),m['edge'],verts=12)
        cyl('StackMouth',.15,.011,(1.13,y,1.948),m['dark'],verts=12)
        beam('TurbineSupport',(.55,y,.2),(.55,y,.5),.15,m['armour'])
    for y in (-.75,0,.75):
        box('SideTeamPanel',(.027,.42,.27),(-1.255,y,.93),m['team'])
        for z in (.64,.72,.80): box('HallLouvre',(.029,.43,.028),(-1.275,y,z),m['steel'],bevel=0)
    facet('Transformer',(.50,.51,.62),(-1.0,-.97,.51),m['dark'],taper=.04)
    for x in (-1.17,-1.04,-.91): cyl('Insulator',.05,.22,(x,-.97,.88),m['utility'],verts=8)
    join('power_plant'); finish('power_plant',10000)


def barracks():
    m=start(); slab(3,3,m)
    facet('BarrackBlock',(2.30,1.56,.95),(0,.31,.665),m['armour'],taper=.055)
    roof_gable('FoldedRoof',0,.31,1.16,2.43,1.69,.20,m['edge'])
    for x in (-.78,.78): box('RoofTeamPanel',(.32,1.43,.035),(x,.31,1.255),m['team'],rot=(0,-9 if x>0 else 9,0))
    # Covered muster porch and reinforced entrance divide the front mass.
    box('Porch',(2.34,.56,.09),(0,-.82,.225),m['steel'])
    for x in (-1.02,0,1.02):
        beam('PorchPost',(x,-1.12,.27),(x,-1.12,1.02),.09,m['edge'])
        beam('PorchBrace',(x,-1.12,.83),(x,-.66,1.09),.048,m['steel'])
    facet('PorchAwning',(2.4,.64,.10),(0,-.81,1.09),m['edge'],taper=.04)
    for x in (-.73,.73):
        box('Window',(.51,.025,.25),(x,-.478,.85),m['glass'],bevel=0)
        for xx in (-.12,.12): box('WindowBar',(.035,.04,.27),(x+xx,-.494,.85),m['edge'],bevel=0)
    box('PersonnelDoor',(.46,.045,.74),(0,-.481,.61),m['dark'])
    box('DoorPanel',(.39,.025,.55),(0,-.509,.60),m['steel'])
    box('DoorHeader',(.57,.04,.11),(0,-.515,1.045),m['team'])
    cooling_unit(-.72,.38,1.34,.63,.58,m)
    cyl('Flagpole',.025,1.64,(1.20,1.09,1.10),m['steel'],verts=8)
    wing('Flag',[(1.20,1.07),(.74,1.07),(.74,1.13),(1.20,1.13)],1.79,.27,m['team'])
    crate(.87,-.86,.27,.28,m)
    join('barracks'); finish('barracks',10000)


def supply_center():
    m=start(); slab(4,4,m)
    facet('Warehouse',(2.1,3.14,1.16),(-.71,0,.77),m['armour'],cut=.05,taper=.015)
    roof_gable('WarehouseRoof',-.71,0,1.36,2.2,3.22,.32,m['edge'])
    box('RoofSpine',(.25,2.88,.07),(-.71,0,1.72),m['team'])
    for y in (-1.27,-.62,.03,.68,1.33):
        box('WarehouseRib',(.045,.09,1.05),(.349,y,.8),m['edge'],bevel=.008)
    panel_door(.385,.43,.79,1.28,1.03,m)
    box('LoadingCanopy',(.52,1.58,.08),(.50,.43,1.48),m['team'])
    box('DockPlatform',(.46,1.57,.18),(.57,.43,.26),m['steel'])
    for y in (-.29,1.13): box('DockBumper',(.08,.09,.28),(.829,y,.32),m['utility'])
    cyl('TransferPad',.89,.05,(1.11,-.19,.20),m['dark'],verts=32)
    ring('PadPerimeter',1.11,-.19,.231,.82,.055,m['marking'])
    for y in (-.44,.06): box('CargoPadGuide',(.74,.035,.013),(1.11,y,.233),m['utility'],bevel=0)
    for x,y in ((-.94,-1.39),(-1.44,-1.37),(-1.44,-.86)):
        crate(x,y,.18,.35,m)
    cooling_unit(-.66,.96,1.53,.77,.53,m)
    box('WarehouseSign',(.025,.64,.20),(.387,-1.0,1.12),m['team'])
    join('supply_center'); finish('supply_center',10000)


def motor_pool():
    m=start(); slab(4,4,m)
    facet('GarageMass',(3.10,2.48,1.28),(0,.40,.84),m['armour'],cut=.05,taper=.025)
    # Two sawtooth roof ridges rather than a flat solid cube.
    for x in (-.80,.80):
        roof_gable('ServiceRoof',x,.40,1.51,1.60,2.61,.28,m['edge'])
        box('RoofSkylight',(.24,1.72,.03),(x+.20,.41,1.73),m['glass'],rot=(0,18,0))
        box('TeamRoof',(.24,1.38,.04),(x-.20,.32,1.73),m['team'],rot=(0,-18,0))
    for x in (-.70,.70):
        box('GarageRecess',(1.18,.04,1.08),(x,-.855,.76),m['dark'])
        for z in (.33,.53,.73,.93,1.13): box('GarageShutter',(1.04,.055,.125),(x,-.882,z),m['steel'],bevel=0)
        for xx in (-.63,.63): box('BayColumn',(.10,.13,1.28),(x+xx,-.89,.85),m['edge'])
        box('BayLintel',(1.32,.16,.14),(x,-.90,1.44),m['team'])
        box('Ramp',(1.11,.92,.095),(x,-1.40,.21),m['dark'])
        for xx in (-.46,.46): box('RampGuide',(.065,.88,.012),(x+xx,-1.40,.264),m['utility'],bevel=0)
    # Portal crane frame and diagonal bracing; never a gameplay obstruction.
    for y in (-.75,1.35):
        beam('CraneUpright',(-1.48,y,.19),(-1.48,y,2.14),.105,m['steel'])
        beam('CraneBrace',(-1.48,y,.90),(-1.48,y+.28 if y<0 else y-.28,1.85),.06,m['utility'])
    box('CraneRail',(.14,2.24,.14),(-1.48,.31,2.11),m['utility'])
    box('CraneTrolley',(.30,.35,.13),(-1.48,-.25,2.08),m['dark'])
    cyl('FuelTank',.28,.95,(1.68,1.34,.69),m['steel'],verts=16)
    for z in (.28,1.09): cyl('TankBand',.294,.045,(1.68,1.34,z),m['edge'],verts=16)
    cooling_unit(.71,1.01,1.77,.59,.55,m)
    join('motor_pool'); finish('motor_pool',10000)


def airfield():
    m=start(); slab(6,4,m,height=.14)
    box('Runway',(5.38,1.28,.045),(0,-.93,.16),m['dark'],bevel=.01)
    for x in (-2.1,-1.4,-.7,0,.7,1.4,2.1): box('RunwayCentre',(.32,.06,.012),(x,-.93,.19),m['marking'],bevel=0)
    for s in (-1,1):
        for x in (-2.4,2.4):
            for y in (-1.2,-.96,-.72): box('RunwayThreshold',(.15,.09,.012),(x,y,.19),m['marking'],bevel=0)
        for x in (-2.5,-1.25,0,1.25,2.5):
            box('RunwayLamp',(.11,.07,.055),(x,-.93+s*.62,.215),m['glass'],bevel=.006)
    for x in (-1.55,1.59):
        cyl('AircraftStand',.73,.03,(x,.84,.17),m['dark'],verts=32)
        ring('StandCircle',x,.84,.190,.64,.043,m['marking'])
        for yy in (-.25,.25): box('PadBar',(.51,.065,.011),(x,.84+yy,.193),m['team'],bevel=0)
        box('PadCrossbar',(.065,.55,.011),(x,.84,.194),m['team'],bevel=0)
    facet('ControlTower',(.77,.78,2.15),(.06,1.29,1.215),m['armour'],taper=.16)
    facet('ControlCab',(1.15,1.15,.45),(.06,1.29,2.52),m['glass'],taper=.13)
    facet('TowerRoof',(1.25,1.23,.14),(.06,1.29,2.83),m['team'],taper=.09)
    for s in (-1,1):
        beam('TowerCabPillar',(.52,1.29+s*.46,2.27),(.52,1.29+s*.46,2.77),.064,m['edge'])
    cyl('TowerBeacon',.06,.18,(.06,1.29,3.015),m['marking'],verts=8)
    facet('UtilityHangar',(1.17,.75,.56),(-2.22,1.51,.47),m['armour'],taper=.07)
    roof_gable('HangarRoof',-2.22,1.51,.78,1.27,.79,.16,m['edge'])
    box('HangarDoor',(.027,.46,.40),(-1.625,1.51,.49),m['dark'])
    box('TowerTeam',(.026,.47,.70),(.425,1.29,1.43),m['team'])
    join('airfield'); finish('airfield',10000)


def strategy_center():
    m=start(); slab(4,4,m)
    facet('OperationsBunker',(2.99,2.96,.97),(0,0,.665),m['armour'],cut=.18,taper=.12)
    facet('Parapet',(2.76,2.76,.12),(0,0,1.19),m['edge'],cut=.18,taper=.01)
    cyl('RadomeCollar',1.045,.18,(0,0,1.33),m['steel'],verts=16)
    # Faceted hemispherical radome with visible broad seams at RTS scale.
    vertices=[(0,0,2.33)]; rings=4; sides=16
    for j in range(1,rings+1):
        angle=j*math.pi/2/rings
        for i in range(sides):
            a=i*math.tau/sides
            vertices.append((1.00*math.sin(angle)*math.cos(a),1.00*math.sin(angle)*math.sin(a),1.36+.97*math.cos(angle)))
    faces=[(0,1+i,1+(i+1)%sides) for i in range(sides)]
    for j in range(rings-1):
        for i in range(sides):
            a=1+j*sides+i; b=1+j*sides+(i+1)%sides
            faces.append((a,b,b+sides,a+sides))
    dome=mesh('FacetedRadome',vertices,faces,m['edge'])
    dome.data.materials.append(m['team'])
    for i,poly in enumerate(dome.data.polygons):
        if i%sides in (2,3,10,11): poly.material_index=1
    panel_door(1.415,-.44,.69,.66,.78,m)
    window_band(1.43,.60,.87,.71,.22,m)
    for x,y,height in ((1.23,1.23,1.77),(-1.23,-1.23,1.39)):
        facet('CommsPedestal',(.37,.37,.31),(x,y,1.255),m['edge'],taper=.1)
        mast(x,y,1.38,height-.27,m,.31)
    for s in (-1,1):
        box('BunkerBand',(1.68,.035,.18),(0,s*1.409,.90),m['team'])
    join('strategy_center'); finish('strategy_center',10000)


def drop_zone():
    m=start(); slab(3,3,m,height=.14)
    cyl('ReceivingPad',1.16,.035,(0,0,.162),m['dark'],verts=32)
    ring('LandingPerimeter',0,0,.187,1.10,.045,m['marking'])
    for x in (-.3,.3): box('LandingHBar',(.11,.86,.012),(x,0,.190),m['marking'],bevel=0)
    box('LandingHCross',(.64,.11,.012),(0,0,.191),m['marking'],bevel=0)
    # Low deployable pad apron with four conspicuous blue corner guides.
    for x in (-.77,.77):
        for y in (-.77,.77):
            box('PadGuide',(.23,.045,.013),(x,y,.188),m['team'],bevel=0,rot=(0,0,45 if x*y<0 else -45))
    facet('ReceivingControl',(.74,.75,.70),(1.015,1.015,.525),m['armour'],taper=.09)
    facet('ControlRoof',(.86,.85,.10),(1.015,1.015,.95),m['team'],taper=.06)
    window_band(1.36,1.01,.74,.45,.18,m)
    box('ControlDoor',(.045,.31,.44),(1.396,1.02,.42),m['steel'])
    cyl('BeaconMast',.042,1.26,(-1.1,1.1,.82),m['steel'],verts=8)
    cyl('BeaconGuard',.10,.17,(-1.1,1.1,1.53),m['edge'],verts=8)
    cyl('BeaconLight',.068,.09,(-1.1,1.1,1.66),m['utility'],verts=8)
    for x,y in ((-.98,-1.0),(-.48,-1.035)):
        crate(x,y,.14,.34,m)
    join('drop_zone'); finish('drop_zone',10000)


def sentry_battery():
    m=start()
    facet('Foundation',(1.8,1.8,.19),(0,0,.095),m['steel'],cut=.14,taper=.01)
    facet('ArmouredPedestal',(1.33,1.33,.53),(0,0,.47),m['armour'],cut=.2,taper=.24)
    for s in (-1,1):
        box('BaseTeam',(.64,.031,.13),(0,s*.62,.40),m['team'])
        facet('Anchor',(.30,.30,.13),(s*.62,.60,.26),m['edge'],taper=.2)
        facet('Anchor',(.30,.30,.13),(s*.62,-.60,.26),m['edge'],taper=.2)
    cyl('TraverseWell',.47,.115,(0,0,.77),m['dark'],verts=16)
    hull=join('Hull'); snap=om.new_parts()
    cyl('TraverseRing',.43,.10,(0,0,.87),m['steel'],verts=16)
    facet('GunHousing',(.71,.64,.32),(-.015,0,1.096),m['armour'],taper=.20)
    for s in (-1,1):
        box('TurretTeam',(.46,.026,.10),(-.025,s*.303,1.14),m['team'])
        cyl('GunSleeve',.072,.32,(.36,s*.115,1.12),m['edge'],verts=10,rot=(0,90,0))
        cyl('Autocannon',.041,.51,(.746,s*.115,1.12),m['steel'],verts=8,rot=(0,90,0))
        cyl('Muzzle',.061,.064,(1.014,s*.115,1.12),m['dark'],verts=8,rot=(0,90,0))
        facet('MissilePod',(.40,.22,.22),(-.045,s*.44,1.25),m['edge'],taper=.09)
        for z in (1.205,1.295): cyl('MissileAperture',.040,.025,(.173,s*.44,z),m['dark'],verts=8,rot=(0,90,0))
    facet('Sight',(.23,.20,.11),(-.09,0,1.333),m['steel'],taper=.1)
    box('SightGlass',(.018,.13,.055),(.034,0,1.34),m['glass'])
    articulate('Turret',(0,0,.85),hull,snap); finish('sentry_battery',10000)


def orbital_uplink():
    m=start(); slab(4,4,m)
    facet('UplinkBunker',(2.56,2.55,.93),(0,0,.645),m['armour'],cut=.16,taper=.10)
    facet('BunkerRim',(2.39,2.38,.13),(0,0,1.18),m['edge'],cut=.16,taper=.025)
    panel_door(1.24,-.25,.68,.79,.83,m)
    for s in (-1,1):
        box('UplinkTeam',(1.59,.035,.21),(0,s*1.207,.87),m['team'])
    cyl('AzimuthBase',.46,.28,(0,0,1.4),m['steel'],verts=16)
    for s in (-1,1):
        beam('DishYoke',(0,s*.34,1.50),(0,s*.44,2.11),.15,m['edge'])
        cyl('ElevationBearing',.17,.13,(0,s*.46,2.04),m['dark'],verts=12,rot=(90,0,0))
    snap=om.new_parts()
    # A real concave reflector, not a flat disc: concentric polygon rings.
    segments=32; rings=(.0,.34,.72,1.08,1.43); vertices=[]
    for radius in rings:
        for i in range(segments):
            a=i*math.tau/segments
            vertices.append((radius*math.cos(a),radius*math.sin(a),2.01+.44*(radius/1.43)**2))
    faces=[]
    for j in range(len(rings)-1):
        for i in range(segments):
            a=j*segments+i;b=j*segments+(i+1)%segments
            faces.append((a,b,b+segments,a+segments))
    dish=mesh('ParabolicReflector',vertices,faces,m['edge']); dish.data.materials.append(m['team'])
    for i,face in enumerate(dish.data.polygons):
        if i%segments in (3,4,19,20): face.material_index=1
    ring('DishRim',0,0,2.452,1.445,.055,m['steel'],segments)
    for a in (0,120,240):
        angle=math.radians(a)
        rod('FeedTripod',(1.2*math.cos(angle),1.2*math.sin(angle),2.32),(0,0,3.09),.029,m['steel'])
        rod('ReflectorBackBrace',(0,0,1.94),(1.28*math.cos(angle),1.28*math.sin(angle),2.33),.043,m['steel'])
    facet('FeedHead',(.23,.23,.18),(0,0,3.12),m['dark'],taper=.1)
    pivot=Vector((0,0,2.04)); rotation=Matrix.Rotation(math.radians(18),4,'X')
    for obj in om.parts_since(snap):
        obj.matrix_world=Matrix.Translation(pivot) @ rotation @ Matrix.Translation(-pivot) @ obj.matrix_world
    for x in (-1.49,1.49):
        for y in (-1.49,1.49):
            facet('PerimeterPylon',(.27,.27,1.19),(x,y,.79),m['armour'],taper=.1)
            box('PylonTeam',(.28,.28,.13),(x,y,1.20),m['team'])
            cyl('WarningBeacon',.09,.12,(x,y,1.48),m['utility'],verts=8)
    join('orbital_uplink'); finish('orbital_uplink',10000)


MODELS={name:globals()[name] for name in ('command_post','power_plant','barracks','supply_center',
        'motor_pool','airfield','strategy_center','drop_zone','sentry_battery','orbital_uplink')}
