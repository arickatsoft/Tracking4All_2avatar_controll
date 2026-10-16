import cv2
import threading
import time

TERMINATE = False
global_vars = None

# the capture thread captures images from the WebCam on a separate thread (for performance)
class CaptureThread(threading.Thread):
    index = None
    targetFps = None
    cap = None
    ret = None
    frame = None
    isRunning = False
    counter = 0
    timer = 0.0 
    callback = None

    def __init__(self, globalVarsModule, callback = None, group=None, target=None, name=None, args=(), kwargs={}) -> None:
        super().__init__(group, target, name, args, kwargs)
        global global_vars
        global_vars = globalVarsModule
        self.callback = callback

    def run(self):
        self.index = global_vars.CAM_INDEX
        self.targetFps = global_vars.FPS
        self.cap = cv2.VideoCapture(self.index) # sometimes it can take a while for certain video captures
        if global_vars.USE_CUSTOM_CAM_SETTINGS:
            self.cap.set(cv2.CAP_PROP_FPS, self.targetFps)
            self.cap.set(cv2.CAP_PROP_FRAME_WIDTH,global_vars.WIDTH)
            self.cap.set(cv2.CAP_PROP_FRAME_HEIGHT,global_vars.HEIGHT)

        time.sleep(1)
        
        print("Opened Capture @ %s fps"%str(self.cap.get(cv2.CAP_PROP_FPS)))
        while self.cap is not None and not global_vars.KILL_THREADS:
            self.ret, self.frame = self.cap.read()
            self.isRunning = True
            
            if self.callback is not None: 
                self.callback(self.ret, self.frame)

            if global_vars.DEBUG:
                self.counter = self.counter+1
                if time.time()-self.timer>=3:
                    print("Capture FPS: ",self.counter/(time.time()-self.timer))
                    self.counter = 0
                    self.timer = time.time()
    def stop(self):
        self.cap.release()
        self.cap = None

    def get_fps(self):
        return float(self.cap.get(cv2.CAP_PROP_FPS))

    def get_width(self):
        return int(self.cap.get(cv2.CAP_PROP_FRAME_WIDTH))

    def get_height(self):
        return int(self.cap.get(cv2.CAP_PROP_FRAME_HEIGHT))