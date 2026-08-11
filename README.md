# Mario Kart World Counter Tool
A tool for keeping track of various stats from worldwides in Mario Kart World and optionally uploading them to a designated database.  

## Initial Setup

1. Go to the releases page and download the latest build for your platform https://github.com/ToastedOven/Mario-Kart-World-Counter-Tool/releases

2. Upon first launch, you should be presented with the settings menu, if not check there first.

3. From here, you can setup your API key if plan on uploading your results to the main database. Alternatively, you can enter a custom DB URL to push to if you are doing your own thing. **If you do not plan on uploading to any server, you can skip this step and should turn off "Auto Upload History"**

4. If you plan on running scans for lobby VR, you will need to have Tesseract installed. You can find more info on installing that here: https://tesseract-ocr.github.io/tessdoc/Installation.html

5. If you are running either VR scans or the automatic scans for track options, you need to go through the Camera Setup page to configure your capture card. If you are unsure which index to chose, it should be within the first 10. Make sure to have your capture card plugged in and your Switch 2 running while you are setting this up.

## General Usage

1. If using a capture card, verify it's plugged in and working.

2. Launch program and select your combo

3. Select the starting from location

4. If auto scans are turned off
    1.  Select the 3 options the game provides
    2.  Select the picked track

5. Select your position, or press the disconnected button if you DC'd mid race.

6. Verify your end of race data with the prompt on screen.

## Known Issues/Caveats

- You must manually select your starting course

- You must manually set if a session is new, however this checkbox starts checked so barring disconnects, it's not usually a problem.

- Mirror mode currently causes hiccups for scans, so you must select courses in these situations.

- You must select your finishing position

- VR Scanning sometimes fails. I haven't been able to pinpoint why, and attempting to train a model on just the VR numbers actually has only made it worse.

- You must manually select your combo

## Future Plans

- Scanning in between races to check if user is on character/kart selection to automatically pull up the character selection menu

- Track scanning working even on mirror mode.

- Better OCR scanning for VR

- Automatic scanning for finishing position
