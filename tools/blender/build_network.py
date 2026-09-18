"""Detailed Network assets; preserves original bounds, pivots and material contracts.
Run: blender -b -P tools/blender/build_network.py -- technical marauder
"""
import os
import sys
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
import omlib as om
from network_shapes import palette,finish
from network_units import UNITS
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'../..'))
OUT=os.path.join(ROOT,'game/assets/models/network')
def build(name):
    from network_buildings import BUILDINGS
    builders={**UNITS,**BUILDINGS}
    om.reset_scene();builders[name](palette());finish(name,OUT)
def _builder(name):return lambda:build(name)
BUILDING_NAMES=('command_cell','safehouse','supply_stash','arms_dealer','tunnel_network','black_market','compound','stinger_site','drone_workshop','launch_site')
MODELS={name:_builder(name) for name in (*UNITS,*BUILDING_NAMES)}
if __name__=='__main__':
    argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
    for name in argv or MODELS:build(name)
