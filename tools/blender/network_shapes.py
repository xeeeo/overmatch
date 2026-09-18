"""Network's reusable mesh craft: welded frames, patched canvas and salvaged machinery.

Everything is baked geometry and eight shared materials. +X forward, +Z up.
The bounds and articulation pivots below are the original shipped asset contract.
"""
import math
import os
import bpy
import bmesh
from mathutils import Vector
import omlib as om

BOUNDS = {
 'angry_mob': ((-.75,-.71,.1),(1.25,.81,1.4)), 'arms_dealer': ((-1.9,-1.9,0),(1.9,1.9,1.95)),
 'black_market': ((-1.3,-1.6941,0),(1.3,1.3,1.6182)), 'bomb_truck': ((-1.2,-.74,0),(1.35,.8085,1.65)),
 'command_cell': ((-2.4,-2.4,0),(2.4,2.4,3.8)), 'compound': ((-1.75,-1.75,0),(1.75,1.75,1.7)),
 'drone_workshop': ((-1.2,-1.35,0),(1.2,1.3,2.7)), 'fpv_drone': ((-.3862,-.3862,.06),(.3862,.3862,.27)),
 'fpv_operator': ((-.18,-.37,0),(.55,.37,1.45)), 'ied': ((-.3329,-.3454,0),(.3329,.3454,.105)),
 'launch_site': ((-1.9,-1.9,-.005),(1.9,1.9,1.6042)), 'marauder': ((-1.1,-.88,.09),(1.3,.88,1.4)),
 'radar_van': ((-1.11,-.72,0),(1.15,.72,3.15)), 'raider_quad': ((-.71,-.56,0),(1.05,.56,1.55)),
 'rebel': ((-.18,-.37,0),(.75,.37,1.37)), 'rocket_buggy': ((-.97,-.69,0),(.97,.69,1.3618)),
 'rpg_trooper': ((-.3,-.37,0),(.7,.3923,1.37)), 'saboteur': ((-.35,-.37,0),(.45,.37,1.37)),
 'safehouse': ((-1.2,-1.7,0),(1.2,1.3,1.4)), 'sprayer': ((-1.1,-1.2,.08),(.8,1.2,1.75)),
 'stinger_site': ((-.9448,-.95,-.005),(.9448,.95,1.5171)), 'supply_stash': ((-1.9254,-1.9,0),(1.9,1.9,1.28)),
 'technical': ((-1.1,-.69,0),(1.1,.69,1.525)), 'tunnel_network': ((-.9,-.9,0),(.9,.9,1.175)),
 'worker': ((-.18,-.37,0),(.45,.37,1.31)),
}
PIVOTS = {'technical':(-.5,0,1), 'marauder':(-.1,0,.85), 'rocket_buggy':(-.5,0,.7),
          'stinger_site':(0,0,.6), 'tunnel_network':(0,0,.95)}

def palette():
    return dict(dark=om.material('NetworkRecess',(.035,.041,.036),.94),
      steel=om.material('NetworkSalvage',(.245,.265,.23),.58,.35),
      rust=om.material('NetworkOxide',(.32,.145,.067),.91),
      sand=om.material('NetworkDustPaint',(.47,.37,.225),.88),
      tarp=om.material('NetworkCanvas',(.16,.22,.115),.98),
      light=om.material('NetworkBone',(.67,.57,.36),.83),
      glass=om.material('NetworkOptics',(.025,.17,.18),.27,.18), team=om.team())

def box(n,size,at,m,b=.015,rot=(0,0,0)):
    return om.box(n,size,at,m,bevel=b,rot=rot)

def cyl(n,r,d,at,m,v=12,rot=(0,0,0)):
    return om.cylinder(n,r,d,at,m,verts=v,rot=rot,bevel=0)

def mesh(n,verts,faces,m):
    data=bpy.data.meshes.new(n); data.from_pydata(verts,[],faces); data.update()
    bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
    ob=bpy.data.objects.new(n,data);bpy.context.collection.objects.link(ob);data.materials.append(m);return ob

def beam(n,a,b,r,m,verts=8):
    a,b=Vector(a),Vector(b)
    rotation=tuple(math.degrees(v) for v in (b-a).to_track_quat('Z','Y').to_euler())
    # omlib bakes location too, so rotation must be supplied before that bake.
    return cyl(n,r,(b-a).length,(a+b)/2,m,verts,rotation)

def loft(n,sections,m):
    vertices=[]
    for x,w,z,h in sections:
        vertices += [(x,-w*.78,z),(x,w*.78,z),(x,w,z+.04),(x,w,h-.05),
                     (x,w*.8,h),(x,-w*.8,h),(x,-w,h-.05),(x,-w,z+.04)]
    faces=[tuple(reversed(range(8))),tuple(range(len(vertices)-8,len(vertices)))]
    for k in range(len(sections)-1):
        for i in range(8):
            a=k*8+i;b=k*8+(i+1)%8;faces.append((a,b,b+8,a+8))
    return mesh(n,vertices,faces,m)

def bake(parts):
    for ob in parts:
        bpy.context.view_layer.objects.active=ob
        for mod in list(ob.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)

def join(n,parts,pivot=(0,0,0)):
    bake(parts);return om.join(n,parts,origin=pivot)

def hull():
    return join('Hull',[o for o in bpy.context.scene.objects if o.type=='MESH'])

def turret(name,body,snapshot):
    ob=join('Turret',om.parts_since(snapshot),PIVOTS[name]);om.parent(ob,body);return ob

def finish(name,out):
    parts=[o for o in bpy.context.scene.objects if o.type=='MESH'];bake(parts)
    if name not in PIVOTS:parts=[join(name,parts)]
    # Keep the shipped selection/muzzle envelope. Articulated origins stay fixed.
    bpy.context.view_layer.update()
    coords=[o.matrix_world@v.co for o in parts for v in o.data.vertices]
    lo=[min(v[i] for v in coords) for i in range(3)];hi=[max(v[i] for v in coords) for i in range(3)]
    targetlo,targethi=BOUNDS[name]
    for ob in parts:
        inv=ob.matrix_world.inverted()
        for vertex in ob.data.vertices:
            w=ob.matrix_world@vertex.co
            vertex.co=inv@Vector(tuple(targetlo[i]+(w[i]-lo[i])*(targethi[i]-targetlo[i])/(hi[i]-lo[i]) for i in range(3)))
    triangles=sum(len(p.vertices)-2 for o in parts for p in o.data.polygons)
    mats={s.material for o in parts for s in o.material_slots if s.material}
    surfaces=sum(len({p.material_index for p in o.data.polygons}) for o in parts)
    assert len(mats)<=8 and surfaces<=16,(name,len(mats),surfaces)
    assert triangles<10000,(name,triangles)
    assert name=='ied' or any(m.name=='TeamColour' for m in mats),name
    if name in PIVOTS:
        t=bpy.data.objects['Turret'];assert t.parent.name=='Hull';assert (t.location-Vector(PIVOTS[name])).length<1e-5
    os.makedirs(out,exist_ok=True);om.export_glb(os.path.join(out,name+'.glb'))
    print(f'[network] {name}: {triangles} triangles / {len(mats)} materials / {surfaces} surfaces')

def rivets(a,b,count,m,r=.014):
    a,b=Vector(a),Vector(b)
    for i in range(count):
        pos=a+(b-a)*i/max(1,count-1);cyl('WeldBolt',r,.022,pos,m,6)

def wheel(x,y,r,m,depth=.23):
    cyl('OffroadTyre',r,depth,(x,y,r),m['dark'],16,(90,0,0))
    side=1 if y>0 else -1
    cyl('SteelRim',r*.62,depth+.025,(x,y,r),m['steel'],12,(90,0,0))
    cyl('RecessHub',r*.28,depth+.035,(x,y,r),m['dark'],10,(90,0,0))
    cyl('AxleCap',r*.15,depth+.05,(x,y,r),m['sand'],8,(90,0,0))
    for i in range(12):
        a=i*math.tau/12
        box('TreadBlock',(.10,depth+.015,.032),(x+math.sin(a)*r,y,r+math.cos(a)*r),m['dark'],0,(0,math.degrees(a),0))
    for i in range(5):
        a=i*math.tau/5
        cyl('WheelLug',.014,.02,(x+math.cos(a)*r*.41,y+side*(depth/2+.023),r+math.sin(a)*r*.41),m['light'],6,(90,0,0))

def tracks(length,y,r,m,cx=0):
    for side in (-1,1):
        yy=side*y
        box('TrackBed',(length,.28,.32),(cx,yy,r),m['dark'],.08)
        for i in range(5):
            x=cx-length*.37+i*length*.185
            cyl('RoadWheel',r*.76,.30,(x,yy,r),m['steel'],10,(90,0,0))
            cyl('WheelCap',r*.32,.325,(x,yy,r),m['sand'],8,(90,0,0))
        for i in range(13):
            x=cx-length*.47+length*.94*i/12
            for z in (r-.17,r+.17):box('TrackShoe',(.09,.32,.045),(x,yy,z),m['steel'] if i%5==0 else m['dark'],0)
        for x in (cx-length/2,cx+length/2):
            for a in (-45,0,45):box('TrackTurn',(.07,.32,.09),(x,yy,r+a*.002),m['dark'],0,(0,a,0))

def crate(x,y,z,w,m,rot=0):
    box('SalvageCrate',(w,w*.78,w*.68),(x,y,z+w*.34),m['sand'],.025,rot=(0,0,rot))
    for xx in (-w*.32,w*.32):box('CrateStrap',(.04,w*.81,w*.70),(x+xx,y,z+w*.34),m['steel'],0)
    box('CrateMark',(w*.26,.012,w*.16),(x,y-w*.405,z+w*.36),m['light'],0)

def drum(x,y,z,r,m):
    cyl('FuelDrum',r,r*2.5,(x,y,z+r*1.25),m['rust'],12)
    for dz in (.2,1.2,2.25):cyl('DrumRib',r*1.035,.04,(x,y,z+r*dz),m['steel'],12)
    cyl('Filler',r*.2,.025,(x+r*.4,y,z+r*2.51),m['dark'],8)

def canvas(center,size,m):
    x,y,z=center;w,h=size
    verts=[]
    for j in range(4):
        for i in range(5):
            xx=-w/2+w*i/4;yy=-h/2+h*j/3
            sag=.06*math.sin(i*math.pi/4)+.03*math.sin(j*math.pi/3)
            verts.append((x+xx,y+yy,z-sag+.055*(i%2)))
    faces=[]
    for j in range(3):
        for i in range(4):a=j*5+i;faces.append((a,a+1,a+6,a+5))
    ob=mesh('StretchedCanvas',verts,faces,m['tarp']);solid=ob.modifiers.new('CanvasThickness','SOLIDIFY');solid.thickness=.02
    for xx in (-w/2,w/2):
        beam('CanvasEdge',(x+xx,y-h/2,z),(x+xx,y+h/2,z),.017,m['light'],6)
    box('CanvasPatch',(w*.25,h*.28,.013),(x+w*.18,y-h*.15,z+.002),m['sand'],0,rot=(0,0,8))

def sandbags(w,h,m,layers=2):
    for k in range(layers):
        for side in (-1,1):
            for i in range(max(2,int(w/.5))):
                x=-w/2+.25+i*(w-.5)/(max(2,int(w/.5))-1)
                box('Sandbag',(.47,.29,.18),(x,side*(h/2-.14),.10+k*.15),m['sand'] if (i+k)%3 else m['light'],.065,rot=(0,0,(i%3-1)*4))
            for i in range(max(1,int((h-.6)/.5))):
                y=-h/2+.50+i*.47
                box('EndBag',(.29,.47,.18),(side*(w/2-.14),y,.10+k*.15),m['sand'],.06)

def corrugated(center,size,m):
    x,y,z=center;w,h=size
    box('RoofBacking',(w,h,.045),center,m['rust'],0)
    for i in range(max(3,int(w/.16))):
        xx=x-w/2+.07+i*(w-.14)/(max(3,int(w/.16))-1)
        box('RoofRidge',(.035,h,.035),(xx,y,z+.031),m['steel'] if i%6==0 else m['rust'],0)

def door(x,y,z,w,h,m,axis='y'):
    size=(w,.035,h) if axis=='y' else (.035,w,h)
    box('DoorRecess',size,(x,y,z+h/2),m['dark'],.01)
    for i in range(5):
        if axis=='y':box('DoorSlat',(w*.94,.04,.026),(x,y-.025,z+h*(i+1)/6),m['steel'],0)
        else:box('DoorSlat',(.04,w*.94,.026),(x+.025,y,z+h*(i+1)/6),m['steel'],0)

def panel(x,y,z,w,h,m):
    box('RepairPanel',(w,.04,h),(x,y,z),m['rust'],.009,rot=(0,0,2))
    for xx in (-w*.38,w*.38):
        for zz in (-h*.35,h*.35):cyl('PanelRivet',.017,.05,(x+xx,y,z+zz),m['light'],6,(90,0,0))
