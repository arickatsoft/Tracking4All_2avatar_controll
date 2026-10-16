# Internally used, don't mind this.
KILL_THREADS = False

DEBUG = True # more verbose and display preview if true

# Change TCP connection settings (must match Unity side)
HOST = '127.0.0.1'
PORT = 52733

# WebCam settings (not all WebCams support all frame rates and resolutions)
CAM_INDEX = 0 # OpenCV2 webcam index, change for using another webcam.

USE_CUSTOM_CAM_SETTINGS = False
FPS = 30
WIDTH = 1280
HEIGHT = 720