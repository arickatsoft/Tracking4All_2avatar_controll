
from networkedwebcam.networkwebcam import NetworkWebCam
import networkedwebcam.global_vars as global_vars
import time
from sys import exit

def main():
    cam = NetworkWebCam()
    cam.start()

    i = input()
    print("Exiting…")        
    global_vars.KILL_THREADS = True
    time.sleep(0.5)
    exit()