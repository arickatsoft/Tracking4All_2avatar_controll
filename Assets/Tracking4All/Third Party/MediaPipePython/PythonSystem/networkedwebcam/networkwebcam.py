import networkedwebcam.global_vars as global_vars
from common.clientTCP import ClientTCP # use tcp for reliability and larger packet support 
from common.captureThread import CaptureThread

import time
import cv2
import struct

class CameraInitializationPacketData:
    def __init__(self, index, width, height, fps):
        self.index = index
        self.width = width
        self.height = height
        self.fps = fps

    def to_bytes(self):
        return struct.pack('!I I I f', self.index, self.width, self.height, self.fps)

class NetworkWebCam:

    def __init__(self):
        pass

    def start(self):
        try:
            # set up networking
            self.client = ClientTCP(global_vars.HOST,global_vars.PORT)

            # set up capture thread with callback
            self.capture = CaptureThread(global_vars, self.callback)
            self.capture.start()

            # wait verbosely for capture
            while not global_vars.KILL_THREADS and self.capture.isRunning==False:
                print("Waiting for camera and capture thread.")
                time.sleep(0.5)
            print("Beginning capture...")

            while not global_vars.KILL_THREADS and self.capture.isRunning==True:
                # connect if not connected
                if not self.client.connected:
                    self.client.connect()
                        
                    # send initialization message
                    if self.client.connected:
                        cameraSettings = CameraInitializationPacketData(
                            self.capture.index, 
                            self.capture.get_width(), self.capture.get_height(),
                            self.capture.get_fps())

                        b = cameraSettings.to_bytes()
                        self.send_initialization(b)
                        print("Sent camera initialization... ("+str(len(b))+")")
                    pass

        except:
            print("Releasing resources...")
            self.capture.stop()
            self.client.disconnect()
        
            
    def callback(self, ret, frame):
        if self.client.connected==False:
            return

        #frame = cv2.flip(frame, 1)

        # Encode frame as a PNG
        ret, png = cv2.imencode(".png", frame)
        if not ret:
            print("Failed to encode image...")
            return

        png_bytes = png.tobytes()
        self.send_image(png_bytes)

    def send_initialization(self, data : bytes):
        # send initialization settings
        self.client.sendBytes(117, data)
        pass

    def send_image(self, data : bytes):
        # send image settings
        self.client.sendBytes(118, data)
        pass

    pass