import socket
import threading
import time
import struct

class ClientTCP(threading.Thread):

    def run(self):
        self.connect()

    def __init__(self, ip, port) -> None:
        threading.Thread.__init__(self)
        self.ip = ip
        self.port = port
        self.connected = False

    def isConnected(self):
        return self.connected

    def sendBytes(self, message_type: int, data: bytes):
        try:
            # Format: length, type, raw data
            data_length = len(data)
            header = struct.pack('!I I', data_length, message_type)
            self.socket.sendall(header + data)
        except Exception as e:
            print(f"Error sending message: {e}")
            self.disconnect()

    def disconnect(self):
        self.connected = False
        try:
            self.socket.close()
        except:
            pass

    def connect(self):
        try:
            self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)  # TCP socket
            print("Attempting Connection...")
            self.socket.connect((self.ip, self.port))
            print("Connected to " + str(self.socket.getpeername()))
            self.connected = True
        except (ConnectionRefusedError, ConnectionResetError) as ex:
            print(f"Connection error: {ex}.")
            time.sleep(1)
