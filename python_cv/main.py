import cv2
import mediapipe as mp
import time

class GestureTracker:
    def __init__(self):
        self.mp_hands = mp.solutions.hands
        self.hands = self.mp_hands.Hands(
            static_image_mode=False,
            max_num_hands=2,
            min_detection_confidence=0.7,
            min_tracking_confidence=0.5
        )
        self.mp_draw = mp.solutions.drawing_utils
        self.pTime = 0
        
        # State variables for dynamic gestures
        self.prev_arrow_state = 0
        self.prev_lightning_state = 0
        self.lightning_y_start = 0

    def fingers_up(self, hand_landmarks):
        fingers = []
        # Thumb (Simple check: is tip further from center than IP joint)
        if hand_landmarks.landmark[self.mp_hands.HandLandmark.THUMB_TIP].x < hand_landmarks.landmark[self.mp_hands.HandLandmark.THUMB_IP].x:
            fingers.append(1)
        else:
            fingers.append(0)
            
        # 4 Fingers
        tip_ids = [self.mp_hands.HandLandmark.INDEX_FINGER_TIP, 
                   self.mp_hands.HandLandmark.MIDDLE_FINGER_TIP, 
                   self.mp_hands.HandLandmark.RING_FINGER_TIP, 
                   self.mp_hands.HandLandmark.PINKY_TIP]
        pip_ids = [self.mp_hands.HandLandmark.INDEX_FINGER_PIP, 
                   self.mp_hands.HandLandmark.MIDDLE_FINGER_PIP, 
                   self.mp_hands.HandLandmark.RING_FINGER_PIP, 
                   self.mp_hands.HandLandmark.PINKY_PIP]
                   
        for i in range(4):
            if hand_landmarks.landmark[tip_ids[i]].y < hand_landmarks.landmark[pip_ids[i]].y:
                fingers.append(1)
            else:
                fingers.append(0)
        return fingers

    def process_frame(self, img):
        img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
        self.results = self.hands.process(img_rgb)
        
        if self.results.multi_hand_landmarks:
            for hand_landmarks in self.results.multi_hand_landmarks:
                self.mp_draw.draw_landmarks(
                    img, hand_landmarks, self.mp_hands.HAND_CONNECTIONS)
                
        return img

    def detect_gestures(self):
        if not self.results.multi_hand_landmarks:
            return None
            
        gesture_detected = None
        
        for idx, hand_landmarks in enumerate(self.results.multi_hand_landmarks):
            fingers = self.fingers_up(hand_landmarks)
            
            # 3. Fortify/Heal Wall - Both hands open (Stop gesture)
            if len(self.results.multi_hand_landmarks) == 2:
                hand2_landmarks = self.results.multi_hand_landmarks[1]
                fingers2 = self.fingers_up(hand2_landmarks)
                if sum(fingers) >= 4 and sum(fingers2) >= 4:
                    gesture_detected = "Fortify_Wall"
                    print("Gesture Detected: Fortify/Heal Wall")
                    # Reset states to avoid false positives
                    self.prev_arrow_state = 0
                    return gesture_detected

            if len(self.results.multi_hand_landmarks) == 1:
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
                    self.lightning_y_start = hand_landmarks.landmark[self.mp_hands.HandLandmark.INDEX_FINGER_TIP].y
                elif self.prev_lightning_state == 1:
                    current_y = hand_landmarks.landmark[self.mp_hands.HandLandmark.INDEX_FINGER_TIP].y
                    # Y goes down as value increases (from 0 top to 1 bottom)
                    if current_y - self.lightning_y_start > 0.2: 
                        gesture_detected = "Lightning_Strike"
                        print("Gesture Detected: Lightning Strike")
                        self.prev_lightning_state = 0
                
        return gesture_detected

def main():
    cap = cv2.VideoCapture(0)
    tracker = GestureTracker()
    
    while True:
        success, img = cap.read()
        if not success:
            break
            
        img = tracker.process_frame(img)
        gesture = tracker.detect_gestures()
        
        if gesture:
            cv2.putText(img, gesture, (10, 120), cv2.FONT_HERSHEY_PLAIN, 3, (0, 255, 0), 3)
        
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
