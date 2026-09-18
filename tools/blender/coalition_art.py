"""Shared Coalition art language, based on the approved Bulwark study.

Faceted load-bearing shapes, dark separated mechanics, broad team panels and
modelled role hardware. No textures, game data or runtime code are required.
"""
import math
import os
import bpy
from mathutils import Vector
import omlib as om
from build_bulwark import mesh, prism, loft, bake, join_at_origin

OUT = os.path.abspath(os.path.join(os.path.dirname(__file__), '../../game/assets/models/coalition'))


def start():
    om.reset_scene()
    return dict(
        armour=om.material('CoalitionArmour', (.19, .235, .205), roughness=.8),
        edge=om.material('CoalitionEdge', (.30, .335, .28), roughness=.74),
        dark=om.material('Mechanics', (.025, .037, .035), roughness=.9),
        steel=om.material('Steel', (.20, .235, .235), roughness=.55, metallic=.4),
        glass=om.material('Optics', (.045, .25, .31), roughness=.24, metallic=.15),
        marking=om.material('Identification', (.75, .77, .63), roughness=.76),
        utility=om.material('Utility', (.43, .285, .07), roughness=.82),
        team=om.team(),
    )


def box(name, size, loc, mat, bevel=.012, rot=(0, 0, 0)):
    return om.box(name, size, loc, mat, bevel=bevel, rot=rot)


def cyl(name, r, depth, loc, mat, verts=12, rot=(0, 0, 0), bevel=0):
    return om.cylinder(name, r, depth, loc, mat, verts=verts, rot=rot, bevel=bevel)


def facet(name, size, loc, mat, cut=.15, taper=.1):
    x, y, z = loc; w, d, h = size
    outline = [(x-w/2+cut*w,y-d/2),(x+w/2-cut*w,y-d/2),(x+w/2,y-d/2+cut*d),
               (x+w/2,y+d/2-cut*d),(x+w/2-cut*w,y+d/2),(x-w/2+cut*w,y+d/2),
               (x-w/2,y+d/2-cut*d),(x-w/2,y-d/2+cut*d)]
    return prism(name, outline, z-h/2, z+h/2, mat, inset=taper)


def beam(name, a, b, width, mat, depth=None):
    a, b = Vector(a), Vector(b)
    axis=(b-a).normalized()
    guide=Vector((0,0,1)) if abs(axis.z)<.95 else Vector((0,1,0))
    u=axis.cross(guide).normalized()*width/2
    v=axis.cross(u.normalized()).normalized()*(depth or width)/2
    vertices=[tuple(p+u*su+v*sv) for p in (a,b) for su,sv in ((-1,-1),(1,-1),(1,1),(-1,1))]
    return mesh(name,vertices,[(3,2,1,0),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],mat)


def rod(name, a, b, radius, mat, verts=8):
    a, b = Vector(a), Vector(b)
    axis=(b-a).normalized()
    guide=Vector((0,0,1)) if abs(axis.z)<.95 else Vector((0,1,0))
    u=axis.cross(guide).normalized()*radius; v=axis.cross(u.normalized()).normalized()*radius
    vertices=[tuple(p+u*math.cos(i*math.tau/verts)+v*math.sin(i*math.tau/verts)) for p in (a,b) for i in range(verts)]
    faces=[tuple(reversed(range(verts))),tuple(range(verts,2*verts))]
    faces += [(i,(i+1)%verts,(i+1)%verts+verts,i+verts) for i in range(verts)]
    return mesh(name,vertices,faces,mat)


def join(name, origin=(0, 0, 0), snapshot=None):
    parts = om.parts_since(snapshot) if snapshot is not None else [o for o in bpy.context.scene.objects if o.type == 'MESH']
    return join_at_origin(name, parts, origin)


def articulate(name, pivot, hull, snapshot):
    node = join(name, pivot, snapshot)
    om.parent(node, hull)
    return node


def finish(name, limit=6000):
    objects = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    bake(objects)
    triangles = sum(len(face.vertices)-2 for obj in objects for face in obj.data.polygons)
    used = {mat.name for obj in objects for mat in obj.data.materials}
    surfaces = sum(len(obj.data.materials) for obj in objects)
    assert 'TeamColour' in used, name + ': missing TeamColour'
    assert triangles <= limit, f'{name}: {triangles} triangles exceeds {limit}'
    assert len(used) <= 8, (name, used)
    assert surfaces <= 16, f'{name}: {surfaces} material surfaces exceeds 16'
    # Runtime writes absolute Y rotations; articulation roots must be neutral.
    for obj in objects:
        if obj.name == 'Turret' or obj.name.startswith('Rotor'):
            assert obj.parent and obj.parent.name == 'Hull', (name, obj.name, 'parent')
            assert max(abs(v) for v in obj.rotation_euler) < 1e-5, (name, obj.name, 'rotation')
    os.makedirs(OUT, exist_ok=True)
    om.export_glb(os.path.join(OUT, name + '.glb'))
    print(f'[coalition-art] {name}: {triangles} triangles, {len(used)} materials, {surfaces} surfaces')


def wheel(x, y, z, r, width, m):
    side = 1 if y > 0 else -1
    cyl('Tyre', r, width, (x,y,z), m['dark'], verts=14, rot=(90,0,0), bevel=.012)
    cyl('WheelRim', r*.63, .025, (x,y+side*width*.51,z), m['steel'], verts=10, rot=(90,0,0))
    cyl('WheelHub', r*.27, .045, (x,y+side*width*.56,z), m['edge'], verts=8, rot=(90,0,0))
    for a in range(0,360,60):
        angle = math.radians(a)
        box('TyreLug', (.065,width+.006,.024), (x+(r-.006)*math.cos(angle),y,z+(r-.006)*math.sin(angle)),
            m['dark'], bevel=0, rot=(0,90-a,0))


def tracks(length, y, width, height, m, x=0, bottom=.07):
    r = height/2; span=length/2-r; z=bottom+r
    for side in (-1,1):
        outer=[]; inner=[]
        for center, angles in ((span, range(-90,91,30)),(-span,range(90,271,30))):
            for a in angles:
                rad=math.radians(a)
                outer.append((x+center+r*math.cos(rad), z+r*math.sin(rad)))
                inner.append((x+center+(r-.055)*math.cos(rad), z+(r-.055)*math.sin(rad)))
        n=len(outer)
        vertices=[(xx,yy,zz) for yy in (side*y-width/2,side*y+width/2) for ring in (outer,inner) for xx,zz in ring]
        faces=[]
        for i in range(n):
            j=(i+1)%n
            faces += [(i,j,2*n+j,2*n+i),(n+i,3*n+i,3*n+j,n+j),(i,n+i,n+j,j),(2*n+i,2*n+j,3*n+j,3*n+i)]
        mesh('ContinuousTrackBelt',vertices,faces,m['dark'])
        for xx in (-span,-span/2,0,span/2,span):
            cyl('RoadWheel',r*.72,width*.94,(x+xx,side*y,z),m['dark'],verts=10,rot=(90,0,0))
            cyl('RoadWheelHub',r*.4,.022,(x+xx,side*(y+width*.51),z),m['steel'],verts=8,rot=(90,0,0))
        for i in range(9):
            xx=x-span+i*span/4
            for zz in (bottom+.005,bottom+height-.005):
                box('TrackLink',(.10,width+.015,.025),(xx,side*y,zz),m['dark'],bevel=0)


def vent(loc, size, m, count=6):
    x,y,z=loc; w,d=size
    box('VentRecess',(w,d,.018),(x,y,z),m['dark'],bevel=0)
    for i in range(count):
        xx=x-w*.43+i*w*.86/(count-1)
        box('CoolingFin',(w*.035,d*.86,.017),(xx,y,z+.014),m['steel'],bevel=0)


def lamp(x, y, z, m):
    box('LampHousing',(.07,.12,.08),(x,y,z),m['dark'])
    box('LampLens',(.011,.08,.033),(x+.041,y,z+.008),m['marking'],bevel=0)


def marks(x,y,z,m):
    for i in range(3):
        box('RecognitionBar',(.025,.12,.012),(x+i*.055,y,z),m['marking'],bevel=0)


def missile(loc, length, radius, m):
    x,y,z=loc
    cyl('MissileBody',radius,length*.8,(x-length*.1,y,z),m['steel'],verts=8,rot=(0,90,0))
    bpy.ops.mesh.primitive_cone_add(vertices=8,radius1=radius,radius2=.009,depth=length*.2,
                                    location=(x+length*.4,y,z),rotation=(0,math.pi/2,0))
    bpy.context.object.data.materials.append(m['marking'])
    for angle in (0,90):
        box('MissileFin',(length*.18,radius*3,.015),(x-length*.34,y,z),m['edge'],bevel=0,rot=(angle,0,0))


def wing(name, outline, z, thickness, mat):
    return prism(name, outline, z-thickness/2,z+thickness/2,mat,inset=.025)
