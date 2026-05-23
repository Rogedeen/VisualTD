import cv2
import mediapipe as mp
import time
import socket
import json
import os
import urllib.request
import base64

class UDPSender:
    def __init__(self, ip="127.0.0.1", port=5052, img_port=5053):
        self.ip = ip
        self.port = port
        self.img_port = img_port
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
            base_options=BaseOptions(model_asset_path="hand_landmarker.task"),
            running_mode=VisionRunningMode.VIDEO,
            num_hands=2)

        self.landmarker = HandLandmarker.create_from_options(options)
        self.pTime = 0
        self.start_time_ms = int(time.time() * 1000)
        
        self.prev_arrow_state = 0
        self.prev_lightning_state = 0
        self.lightning_y_start = 0
        self.results = None
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

    def fingers_up(self, landmarks, handedness="Right"):
        fingers = []
        # Thumb: check based on handedness (MediaPipe handedness is inverted for some reason in some versions)
        # Usually: Left hand thumb up if tip.x > joint.x, Right hand if tip.x < joint.x
        if handedness == "Left":
            if landmarks[4].x > landmarks[3].x: fingers.append(1)
            else: fingers.append(0)
        else:
            if landmarks[4].x < landmarks[3].x: fingers.append(1)
            else: fingers.append(0)
            
        tip_ids = [8, 12, 16, 20]
        pip_ids = [6, 10, 14, 18]
        for i in range(4):
            if landmarks[tip_ids[i]].y < landmarks[pip_ids[i]].y: fingers.append(1)
            else: fingers.append(0)
        return fingers

    def process_frame(self, img, timestamp_ms):
        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=cv2.cvtColor(img, cv2.COLOR_BGR2RGB))
        relative_timestamp = timestamp_ms - self.start_time_ms
        if relative_timestamp <= 0: relative_timestamp = 1
        
        self.results = self.landmarker.detect_for_video(mp_image, relative_timestamp)
        
        if self.results.hand_landmarks:
            for i, hand_landmarks in enumerate(self.results.hand_landmarks):
                # Draw landmarks
                for landmark in hand_landmarks:
                    x = int(landmark.x * img.shape[1])
                    y = int(landmark.y * img.shape[0])
                    cv2.circle(img, (x, y), 5, (255, 0, 255), cv2.FILLED)
                
                # Display handedness and finger count for debugging
                if hasattr(self.results, "handedness") and len(self.results.handedness) > i:
                    label = self.results.handedness[i][0].category_name
                    cv2.putText(img, f"{label}", (int(hand_landmarks[0].x * img.shape[1]), int(hand_landmarks[0].y * img.shape[0])), 
                                cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
        return img

    def detect_gestures(self):
        if not self.results or not self.results.hand_landmarks:
            self.current_raw_gesture = None
            return None
            
        current_time_ms = int(time.time() * 1000)
        raw_gesture = None
        
        if len(self.results.hand_landmarks) == 2:
            h1 = self.results.hand_landmarks[0]
            h2 = self.results.hand_landmarks[1]
            h1_label = self.results.handedness[0][0].category_name
            h2_label = self.results.handedness[1][0].category_name
            fingers1 = self.fingers_up(h1, h1_label)
            fingers2 = self.fingers_up(h2, h2_label)
            wrist_dist = ((h1[0].x - h2[0].x)**2 + (h1[0].y - h2[0].y)**2)**0.5
            
            if wrist_dist < 0.15: raw_gesture = "Fireball"
            elif sum(fingers1) >= 4 and sum(fingers2) >= 4:
                h1_horiz = abs(h1[0].y - h1[9].y) < 0.15
                h2_horiz = abs(h2[0].y - h2[9].y) < 0.15
                h1_vert = (h1[0].y - h1[9].y) > 0.1
                h2_vert = (h2[0].y - h2[9].y) > 0.1
                if (h1_horiz and h2_vert) or (h2_horiz and h1_vert): raw_gesture = "Palm"
                else: raw_gesture = "Fortify_Wall"

        elif len(self.results.hand_landmarks) == 1:
            h1 = self.results.hand_landmarks[0]
            h1_label = self.results.handedness[0][0].category_name
            fingers = self.fingers_up(h1, h1_label)
            is_upright = (h1[0].y - h1[9].y) > 0.1 
            if is_upright:
                if fingers[1] == 1 and fingers[2] == 0 and fingers[3] == 0 and fingers[4] == 1: raw_gesture = "Spiderman"
                else:
                    num_fingers = sum(fingers[1:]) # Index to Pinky
                    if num_fingers == 0: raw_gesture = "Fist"
                    elif num_fingers == 1: raw_gesture = "Index_Up"
                    elif num_fingers == 2: raw_gesture = "Upgrade_2"
                    elif num_fingers == 3: raw_gesture = "Upgrade_3"
                    elif num_fingers == 4:
                        if fingers[0] == 1: raw_gesture = "Palm" # Thumb is also up
                        else: raw_gesture = "Upgrade_4" # Only 4 fingers up

        if raw_gesture != self.current_raw_gesture:
            self.current_raw_gesture = raw_gesture
            self.raw_gesture_start_time = current_time_ms
            
        held_time = current_time_ms - self.raw_gesture_start_time
        gesture_detected = None
        
        if held_time > 1200:
            if self.current_raw_gesture in ["Upgrade_2", "Upgrade_3", "Upgrade_4"]:
                gesture_detected = self.current_raw_gesture
                self.current_raw_gesture = None
            elif self.current_raw_gesture == "Index_Up":
                gesture_detected = "Upgrade_1"
                self.current_raw_gesture = None

        if self.current_raw_gesture == "Palm" and held_time > 800:
            gesture_detected = "Palm"
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

        if self.current_raw_gesture == "Fist" and held_time > 500:
            if self.prev_arrow_state == 0: gesture_detected = "Hold_Fire"
            self.prev_arrow_state = 1
        elif self.current_raw_gesture in ["Upgrade_4", "Palm"] and self.prev_arrow_state == 1:
            gesture_detected = "Arrow_Volley"
            self.prev_arrow_state = 0
            
        if self.current_raw_gesture == "Index_Up" and 300 < held_time < 1200:
            self.prev_lightning_state = 1
            self.lightning_y_start = self.results.hand_landmarks[0][8].y
        elif self.prev_lightning_state == 1:
            if not self.results or len(self.results.hand_landmarks) != 1: self.prev_lightning_state = 0
            else:
                current_y = self.results.hand_landmarks[0][8].y
                if current_y - self.lightning_y_start > 0.2:
                    gesture_detected = "Lightning_Strike"
                    self.prev_lightning_state = 0
        return gesture_detected

def main():
    print("Starting webcam stream (720p 30fps requested)...")
    cap = cv2.VideoCapture(0)
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)
    cap.set(cv2.CAP_PROP_FPS, 30)

    if not cap.isOpened():
        print("Error: Could not open webcam.")
        return

    tracker = GestureTracker()
    udp_sender = UDPSender()
    last_sent_time = 0
    
    print("Main loop started.")
    while True:
        success, img = cap.read()
        if not success:
            time.sleep(0.01)
            continue
            
        timestamp_ms = int(time.time() * 1000)
        img = tracker.process_frame(img, timestamp_ms)
        gesture = tracker.detect_gestures()

        try:
            send_img = cv2.resize(img, (640, 360)) 
            _, buffer = cv2.imencode(".jpg", send_img, [cv2.IMWRITE_JPEG_QUALITY, 85])
            udp_sender.sock.sendto(buffer, (udp_sender.ip, udp_sender.img_port))
        except Exception as e:
            print(f"UDP Frame error: {e}")
        
        current_time = time.time()
        if gesture:
            if current_time - last_sent_time > 1.0:
                udp_sender.send_gesture(gesture)
                print(f"Sent: {gesture}")
                last_sent_time = current_time
        
        time.sleep(0.001)

if __name__ == "__main__":
    main()