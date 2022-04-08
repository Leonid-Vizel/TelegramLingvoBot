import argparse
from model import GFModel
from utils import save_prediction

model = GFModel()

# Initialize parser
parser = argparse.ArgumentParser()
 
# Adding optional argument
parser.add_argument("-t", "--text", help = "Text to correct.")
 
# Read arguments from command line
args = parser.parse_args()
 
if args:
    text = args.__dict__['text']
    pred = model.get_prediction(text)
    save_prediction(pred)