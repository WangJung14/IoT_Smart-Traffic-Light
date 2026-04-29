# File: src/video_manager.py
import cv2
import numpy as np

class VideoGridManager:
    def __init__(self, video_path: str, wait_frame_end: int, total_frames: int):
        # Mở 4 luồng video
        self.caps = {
            0: cv2.VideoCapture(video_path), # North
            1: cv2.VideoCapture(video_path), # East
            2: cv2.VideoCapture(video_path), # South
            3: cv2.VideoCapture(video_path)  # West
        }
        # Cấu hình lật hình để giả lập các làn khác nhau
        self.flip_modes = {0: None, 1: 1, 2: None, 3: 1} 
        
        self.wait_frame_end = wait_frame_end
        self.total_frames = total_frames

    def get_synced_grid(self, light_states: dict) -> np.ndarray:
        """Đồng bộ frame video theo màu đèn và trả về khung hình ghép (Grid)"""
        frames = {}

        for dir_idx, cap in self.caps.items():
            current_frame = int(cap.get(cv2.CAP_PROP_POS_FRAMES))
            state = light_states.get(dir_idx, "RED")

            # Logic Tua Video (Closed-loop)
            if state == "RED" and current_frame >= self.wait_frame_end:
                cap.set(cv2.CAP_PROP_POS_FRAMES, 0)
            elif state == "GREEN":
                if current_frame < self.wait_frame_end:
                    cap.set(cv2.CAP_PROP_POS_FRAMES, self.wait_frame_end)
                elif current_frame >= self.total_frames - 2:
                    cap.set(cv2.CAP_PROP_POS_FRAMES, self.total_frames - 5)

            ret, frame = cap.read()
            if not ret:
                cap.set(cv2.CAP_PROP_POS_FRAMES, 0)
                ret, frame = cap.read()

            # Xử lý hình ảnh (Resize & Flip)
            frame = cv2.resize(frame, (640, 360))
            if self.flip_modes[dir_idx] is not None:
                frame = cv2.flip(frame, self.flip_modes[dir_idx])
            
            frames[dir_idx] = frame

        # Ghép ma trận 2x2
        top_row = np.hstack((frames[0], frames[1]))
        bottom_row = np.hstack((frames[2], frames[3]))
        return np.vstack((top_row, bottom_row))

    def release_all(self):
        """Giải phóng bộ nhớ Camera"""
        for cap in self.caps.values():
            cap.release()