from model import GFModel
from utils import save_prediction
import os

model = GFModel()

while(True):
    if os.path.isfile('text.txt'):
        with open('text.txt', 'r') as f:
            text = f.read()
            if len(text) != 0:
                pred = model.get_prediction(text)
                save_prediction(pred)
                os.remove('text.txt')