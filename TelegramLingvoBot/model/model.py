from typing import List, Dict
from gramformer import Gramformer
from spacy.errors import models_warning
import torch
from transformers.models.auto.configuration_auto import model_type_to_module_name
from utils import sent_splitter, get_corrected_text, text_correction

class GFModel():
    def __init__(self):
        torch.manual_seed(7373)
        if torch.cuda.is_available():
            torch.cuda.manual_seed_all(73)

        self.model = Gramformer(models = 1, use_gpu=False)

    def get_prediction(self, text: str = None) -> str:
        text = sent_splitter(text)
        sents = get_corrected_text(self.model, text)
        return sents