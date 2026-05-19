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
        self.upgrade_state = 0
        self.upgrade_start_time = 0

        # Additional state variables for debouncing
        self.current_raw_gesture = None
        self.raw_gesture_start_time = 0

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
            self.current_raw_gesture = None
            return None
            
        current_time_ms = int(time.time() * 1000)
        raw_gesture = None
        
        # 1. GET INSTANT RAW GESTURE
        if len(self.results.hand_landmarks) == 2:
            h1 = self.results.hand_landmarks[0]
            h2 = self.results.hand_landmarks[1]
            fingers1 = self.fingers_up(h1)
            fingers2 = self.fingers_up(h2)
            
            # Distance between wrists
            wrist_dist = ((h1[0].x - h2[0].x)**2 + (h1[0].y - h2[0].y)**2)**0.5
            
            if wrist_dist < 0.15:
                raw_gesture = "Fireball"
            elif sum(fingers1) >= 4 and sum(fingers2) >= 4:
                # Time out (T-shape)
                h1_horiz = abs(h1[0].y - h1[9].y) < 0.15
                h2_horiz = abs(h2[0].y - h2[9].y) < 0.15
                h1_vert = (h1[0].y - h1[9].y) > 0.1
                h2_vert = (h2[0].y - h2[9].y) > 0.1
                
                if (h1_horiz and h2_vert) or (h2_horiz and h1_vert):
                    raw_gesture = "Time_Out"
                else:
                    raw_gesture = "Fortify_Wall"

        elif len(self.results.hand_landmarks) == 1:
            h1 = self.results.hand_landmarks[0]
            fingers = self.fingers_up(h1)
            
            # Ensure hand is mostly upright (wrist below middle finger)
            is_upright = (h1[0].y - h1[9].y) > 0.1 
            
            if is_upright:
                if fingers[1] == 1 and fingers[2] == 0 and fingers[3] == 0 and fingers[4] == 1:
                    raw_gesture = "Spiderman"
                else:
                    num_fingers = sum(fingers[1:])
                    if num_fingers == 0:
                        raw_gesture = "Fist"
                    elif num_fingers == 4 and sum(fingers) >= 4:
                        raw_gesture = "Open_Hand"
                    elif 1 <= num_fingers <= 4 and fingers[1] == 1:
                        raw_gesture = f"Upgrade_{num_fingers}"
                    elif fingers[1] == 1 and sum(fingers[2:]) == 0:
                        raw_gesture = "Index_Up"

        # 2. DEBOUNCING / TEMPORAL SMOOTHING
        if raw_gesture != self.current_raw_gesture:
            self.current_raw_gesture = raw_gesture
            self.raw_gesture_start_time = current_time_ms
            
        held_time = current_time_ms - self.raw_gesture_start_time
        gesture_detected = None
        
        # Confirm gestures based on held time
        if self.current_raw_gesture == "Time_Out" and held_time > 800:
            gesture_detected = "Time_Out"
            self.current_raw_gesture = None 
            
        elif self.current_raw_gesture == "Fireball" and held_time > 800:
            gesture_detected = "Fireball_Cast"
            self.current_raw_gesture = None
            
        elif self.current_raw_gesture == "Fortify_Wall" and held_time > 800:
            gesture_detected = "Fortify_Wall"
            self.current_raw_gesture = None
            
        elif self.current_raw_gesture == "Spiderman" and held_time > 800:
            gesture_detected = "Spiderman_Cast"
            self.current_raw_gesture = None
            
        elif self.current_raw_gesture and self.current_raw_gesture.startswith("Upgrade_") and held_time > 3000:
            gesture_detected = self.current_raw_gesture
            self.current_raw_gesture = None

        # Arrow Volley logic (Fist -> Open Hand sequence)
        if self.current_raw_gesture == "Fist" and held_time > 500:
            if self.prev_arrow_state == 0:
                gesture_detected = "Hold_Fire"
            self.prev_arrow_state = 1
            
        elif self.current_raw_gesture == "Open_Hand" and self.prev_arrow_state == 1:
            gesture_detected = "Arrow_Volley"
            self.prev_arrow_state = 0
            
        # Lightning Strike logic (Index_Up -> Move down)
        if self.current_raw_gesture == "Index_Up" and held_time > 300:
            self.prev_lightning_state = 1
            self.lightning_y_start = self.results.hand_landmarks[0][8].y
        elif self.prev_lightning_state == 1:
            if not self.results or len(self.results.hand_landmarks) != 1:
                self.prev_lightning_state = 0
            else:
                current_y = self.results.hand_landmarks[0][8].y
                if current_y - self.lightning_y_start > 0.2:
                    gesture_detected = "Lightning_Strike"
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
