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
        self.wiggle_history = []

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

    def are_fingers_stuck_together(self, landmarks):
        # Calculate hand scale: distance between wrist (0) and middle MCP (9) in 3D
        scale = ((landmarks[0].x - landmarks[9].x)**2 + 
                 (landmarks[0].y - landmarks[9].y)**2 + 
                 (landmarks[0].z - landmarks[9].z)**2)**0.5
        if scale == 0:
            return False

        # Calculate 2D Euclidean distances between fingertips
        dist_8_12 = ((landmarks[8].x - landmarks[12].x)**2 + (landmarks[8].y - landmarks[12].y)**2)**0.5
        dist_12_16 = ((landmarks[12].x - landmarks[16].x)**2 + (landmarks[12].y - landmarks[16].y)**2)**0.5
        dist_16_20 = ((landmarks[16].x - landmarks[20].x)**2 + (landmarks[16].y - landmarks[20].y)**2)**0.5

        # Normalize distances
        norm_8_12 = dist_8_12 / scale
        norm_12_16 = dist_12_16 / scale
        norm_16_20 = dist_16_20 / scale

        # Parmak titreşimlerinden etkilenmemek için toplam mesafeyi kontrol ediyoruz
        total_norm_dist = norm_8_12 + norm_12_16 + norm_16_20
        return total_norm_dist < 0.55

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
            
            # 1. Pray (Pause)
            wrist_dist = ((h1[0].x - h2[0].x)**2 + (h1[0].y - h2[0].y)**2)**0.5
            tip_dist = ((h1[12].x - h2[12].x)**2 + (h1[12].y - h2[12].y)**2)**0.5
            if wrist_dist < 0.18 and tip_dist < 0.18:
                h1_upright = h1[12].y < h1[0].y - 0.1
                h2_upright = h2[12].y < h2[0].y - 0.1
                if h1_upright and h2_upright:
                    raw_gesture = "pause"
            
            # 2. Fireball
            if raw_gesture is None and wrist_dist < 0.15:
                raw_gesture = "fireball"
                
            # 3. Fortify (Wall) vs Wiggle (Lightning)
            if raw_gesture is None and sum(fingers1) >= 4 and sum(fingers2) >= 4:
                # Track finger wiggling
                dist_h1 = sum(((h1[i].x - h1[0].x)**2 + (h1[i].y - h1[0].y)**2 + (h1[i].z - h1[0].z)**2)**0.5 for i in [8, 12, 16, 20])
                dist_h2 = sum(((h2[i].x - h2[0].x)**2 + (h2[i].y - h2[0].y)**2 + (h2[i].z - h2[0].z)**2)**0.5 for i in [8, 12, 16, 20])
                scale1 = ((h1[0].x - h1[9].x)**2 + (h1[0].y - h1[9].y)**2 + (h1[0].z - h1[9].z)**2)**0.5
                scale2 = ((h2[0].x - h2[9].x)**2 + (h2[0].y - h2[9].y)**2 + (h2[0].z - h2[9].z)**2)**0.5
                
                if scale1 > 0 and scale2 > 0:
                    total_dist_norm = (dist_h1 / scale1) + (dist_h2 / scale2)
                    self.wiggle_history.append(total_dist_norm)
                    if len(self.wiggle_history) > 10:
                        self.wiggle_history.pop(0)
                
                if len(self.wiggle_history) >= 5:
                    wiggle_val = sum(abs(self.wiggle_history[i] - self.wiggle_history[i-1]) for i in range(1, len(self.wiggle_history)))
                    if wiggle_val > 0.12:
                        raw_gesture = "lightning"
                
                if raw_gesture is None:
                    h1_vert = (h1[0].y - h1[9].y) > 0.1
                    h2_vert = (h2[0].y - h2[9].y) > 0.1
                    if h1_vert and h2_vert:
                        raw_gesture = "fortify"
        elif len(self.results.hand_landmarks) == 1:
            self.wiggle_history.clear()
            h1 = self.results.hand_landmarks[0]
            h1_label = self.results.handedness[0][0].category_name
            fingers = self.fingers_up(h1, h1_label)
            is_upright = (h1[0].y - h1[9].y) > 0.1 
            if is_upright:
                # Sadece 4 ana parmağı (İşaret, Orta, Yüzük, Serçe) sayıyoruz
                num_fingers = sum(fingers[1:])
                if num_fingers == 0: 
                    raw_gesture = "Fist"
                else:
                    if self.prev_arrow_state == 1:
                        raw_gesture = "Spread_Open"
                    else:
                        if num_fingers == 1: 
                            raw_gesture = "upgrade 1"
                        elif num_fingers == 2: 
                            raw_gesture = "upgrade 2"
                        elif num_fingers == 3: 
                            raw_gesture = "upgrade 3"
                        elif num_fingers == 4: 
                            raw_gesture = "upgrade 4"

        if raw_gesture != self.current_raw_gesture:
            self.current_raw_gesture = raw_gesture
            self.raw_gesture_start_time = current_time_ms
            
        held_time = current_time_ms - self.raw_gesture_start_time
        gesture_detected = None
        
        if self.current_raw_gesture in ["upgrade 1", "upgrade 2", "upgrade 3", "upgrade 4"] and held_time > 1200:
            gesture_detected = self.current_raw_gesture
            self.current_raw_gesture = None

        if self.current_raw_gesture == "pause" and held_time > 800:
            gesture_detected = "pause"
            self.current_raw_gesture = None 
        elif self.current_raw_gesture == "fireball" and held_time > 800:
            gesture_detected = "fireball"
            self.current_raw_gesture = None
        elif self.current_raw_gesture == "fortify" and held_time > 800:
            gesture_detected = "fortify"
            self.current_raw_gesture = None
        elif self.current_raw_gesture == "lightning" and held_time > 800:
            gesture_detected = "lightning"
            self.current_raw_gesture = None

        if self.current_raw_gesture == "Fist" and held_time > 500:
            if self.prev_arrow_state == 0: gesture_detected = "hold fire"
            self.prev_arrow_state = 1
        elif self.prev_arrow_state == 1 and self.current_raw_gesture not in ["Fist", None]:
            gesture_detected = "arrow volley"
            self.prev_arrow_state = 0
            self.current_raw_gesture = None
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