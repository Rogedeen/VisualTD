import cv2
import mediapipe as mp
import time
import socket
import json
import os
import urllib.request

class UDPSender:
    def __init__(self, ip="127.0.0.1", port=5052):
        self.ip = ip
        self.port = port
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        
    def send_gesture(self, gesture_name):
        data = {"gesture": gesture_name}
        json_data = json.dumps(data)
        self.sock.sendto(json_data.encode("utf-8"), (self.ip, self.port))

class GestureTracker:
    def __init__(self):
        self.download_model()
        
        BaseOptions = mp.tasks.BaseOptions
        HandLandmarker = mp.tasks.vision.HandLandmarker
        HandLandmarkerOptions = mp.tasks.vision.HandLandmarkerOptions
        VisionRunningMode = mp.tasks.vision.RunningMode

        options = HandLandmarkerOptions(
            base_options=BaseOptions(model_asset_path='hand_landmarker.task'),
            running_mode=VisionRunningMode.VIDEO,
            num_hands=2)

        self.landmarker = HandLandmarker.create_from_options(options)
        self.pTime = 0
        self.start_time_ms = int(time.time() * 1000)
        
        # State variables for dynamic gestures
        self.prev_arrow_state = 0
        self.prev_lightning_state = 0
        self.lightning_y_start = 0
        self.results = None

    def download_model(self):
        model_path = "hand_landmarker.task"
        if not os.path.exists(model_path):
            print("Downloading MediaPipe hand landmarker model...")
            urllib.request.urlretrieve(
                "https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/1/hand_landmarker.task",
                model_path)
            print("Download complete.")

    def fingers_up(self, landmarks):
        fingers = []
        
        # Thumb
        if landmarks[4].x < landmarks[3].x:
            fingers.append(1)
        else:
            fingers.append(0)
            
        # 4 Fingers
        tip_ids = [8, 12, 16, 20]
        pip_ids = [6, 10, 14, 18]
                   
        for i in range(4):
            if landmarks[tip_ids[i]].y < landmarks[pip_ids[i]].y:
                fingers.append(1)
            else:
                fingers.append(0)
        return fingers

    def process_frame(self, img, timestamp_ms):
        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=cv2.cvtColor(img, cv2.COLOR_BGR2RGB))
        # Ensure timestamp is strictly increasing and relative to start
        relative_timestamp = timestamp_ms - self.start_time_ms
        if relative_timestamp <= 0:
            relative_timestamp = 1
        
        self.results = self.landmarker.detect_for_video(mp_image, relative_timestamp)
        
        # Draw landmarks using OpenCV
        if self.results.hand_landmarks:
            for hand_landmarks in self.results.hand_landmarks:
                for landmark in hand_landmarks:
                    x = int(landmark.x * img.shape[1])
                    y = int(landmark.y * img.shape[0])
                    cv2.circle(img, (x, y), 5, (255, 0, 255), cv2.FILLED)
                
        return img

    def detect_gestures(self):
        if not self.results or not self.results.hand_landmarks:
            return None
            
        gesture_detected = None
        
        # Multiple hands check for Fortify
        if len(self.results.hand_landmarks) == 2:
            fingers1 = self.fingers_up(self.results.hand_landmarks[0])
            fingers2 = self.fingers_up(self.results.hand_landmarks[1])
            if sum(fingers1) >= 4 and sum(fingers2) >= 4:
                gesture_detected = "Fortify_Wall"
                print("Gesture Detected: Fortify/Heal Wall")
                self.prev_arrow_state = 0
                return gesture_detected

        if len(self.results.hand_landmarks) == 1:
            landmarks = self.results.hand_landmarks[0]
            fingers = self.fingers_up(landmarks)
            
            # 1. Arrow Volley: Fist closed -> Hand opened
            if sum(fingers) == 0:
                self.prev_arrow_state = 1
            elif sum(fingers) >= 4 and self.prev_arrow_state == 1:
                gesture_detected = "Arrow_Volley"
                print("Gesture Detected: Arrow Volley")
                self.prev_arrow_state = 0
                
            # 2. Lightning Strike: Index finger pointing up -> Swift downward motion
            index_up_only = (fingers[1] == 1 and sum(fingers[2:]) == 0)
            if index_up_only:
                self.prev_lightning_state = 1
                self.lightning_y_start = landmarks[8].y # index finger tip
            elif self.prev_lightning_state == 1:
                current_y = landmarks[8].y
                if current_y - self.lightning_y_start > 0.2: 
                    gesture_detected = "Lightning_Strike"
                    print("Gesture Detected: Lightning Strike")
                    self.prev_lightning_state = 0
                
        return gesture_detected

def main():
    cap = cv2.VideoCapture(0)
    tracker = GestureTracker()
    udp_sender = UDPSender()
    last_sent_time = 0
    
    while True:
        success, img = cap.read()
        if not success:
            break
            
        timestamp_ms = int(time.time() * 1000)
        img = tracker.process_frame(img, timestamp_ms)
        gesture = tracker.detect_gestures()
        
        current_time = time.time()
        if gesture:
            cv2.putText(img, gesture, (10, 120), cv2.FONT_HERSHEY_PLAIN, 3, (0, 255, 0), 3)
            # 1 second cooldown to prevent spamming UDP
            if current_time - last_sent_time > 1.0:
                udp_sender.send_gesture(gesture)
                print(f"Sent UDP packet: {gesture}")
                last_sent_time = current_time
        
        # Calculate FPS
        cTime = time.time()
        fps = 1 / (cTime - tracker.pTime) if tracker.pTime != 0 else 0
        tracker.pTime = cTime
        
        cv2.putText(img, f'FPS: {int(fps)}', (10, 70), cv2.FONT_HERSHEY_PLAIN, 3, (255, 0, 255), 3)
        cv2.imshow("Image", img)
        
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    cap.release()
    cv2.destroyAllWindows()

if __name__ == "__main__":
    main()
