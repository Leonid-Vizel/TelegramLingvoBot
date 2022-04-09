from typing import List, Dict
import re
from spacy.errors import models_warning

from transformers.models.auto.configuration_auto import model_type_to_module_name

def sent_splitter(text: str = None) -> List[str]:

    """Split text to the list of sentences."""

    assert text != None, "no text to split!"

    return re.split('(?<=[.!?]) +',text)

def get_correction_highlight(model, text: str = None) -> List[str]:

    """Get corrected text with highlighted incorrect parts."""

    assert text != None, "no text to highlight!"

    highlight_sents = []
    for influent_sentence in text:
        corrected_sentences = model.correct(influent_sentence, max_candidates=1)
        for corrected_sentence in corrected_sentences:
            highlight_sentence = model.highlight(influent_sentence, corrected_sentence)
            highlight_sents.append(highlight_sentence)
    return highlight_sents

def text_correction(text: List[str] = None) -> List[str]:
        
    """Incorrect parts in bold"""

    assert text != None, "no text to  correct!"

    corrected = list()
    for sent in text:
        before_value  = re.findall(r'<[cda].*? edit=\'',sent)
        with_value  = re.findall(r'<[cda].*? edit=\'.*?\'', sent)  
        num_val = len(before_value)
        values = ['']*num_val
        for i in range(num_val):
            values[i] = '**' + with_value[i][len(before_value[i]):-1] + '**'
        for i in range(num_val):
            sent = re.sub(r'<[cda].*?>.*?</[cda]>',values[i], sent, 1)
        corrected.append(sent)
    corrected_text = " ".join(corrected)
    return corrected_text

def save_prediction(text: str):
    text_file = open("prediction.txt", "w")
    text_file.write(text)
    text_file.close()

