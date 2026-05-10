# Drawing CP - Đồ Án Lập Trình Đa Nền Tảng

## 1. Thông tin nhóm
| STT | Họ và Tên | MSSV |
|---|---|---|
| 1 | **Nguyễn Minh Thuận** | 23127491 |
| 2 | **Lê Hồ Phi Hoàng** | 22127123 |

* **Nền tảng sử dụng:** .NET MAUI (.NET 10)

---

## 2. Video Demo chức năng
* **Link Video (YouTube Unlisted):** https://youtu.be/CDSBf_oPTuM
* **Ghi chú:** Video thể hiện ứng dụng chạy đồng thời trên môi trường **Windows Desktop** và **Android Mobile**.

---

## 3. Danh sách các chức năng đã thực hiện
Hệ thống đã hoàn thành 100% các yêu cầu của đồ án với kiến trúc tối ưu (Double-Layer Canvas):

### 🖌️ Chức năng vẽ cơ bản
- [x] **Vẽ điểm (Point):** Hỗ trợ chấm các điểm trên khung vẽ.
- [x] **Đường thẳng (Line):** Vẽ đoạn thẳng kết nối hai điểm chạm.
- [x] **Hình chữ nhật & Hình vuông:** Hỗ trợ vẽ Rectangle và Square (tự động giữ tỷ lệ).
- [x] **Hình Ellipse & Hình tròn:** Hỗ trợ vẽ các hình tròn và ellipse mượt mà.

### 🛠️ Chỉnh sửa và Thuộc tính
- [x] **Tô màu (Fill):** Hỗ trợ đổ màu cho các hình đóng (Sử dụng công cụ Select trước khi Fill).
- [x] **Độ dày đường viền:** Slider thay đổi Stroke Thickness thời gian thực cho vật thể đang chọn.
- [x] **Màu đường viền:** Color Picker tùy chỉnh màu sắc cho Stroke.
- [x] **Công cụ Select:** Chọn vật thể để di chuyển (Move) hoặc chỉnh sửa thuộc tính.
- [x] **Xóa (Delete) & Hoàn tác (Undo):** Quản lý và khôi phục các đối tượng đã vẽ.

### 💾 Lưu trữ và Xuất bản
- [x] **Lưu/Mở tệp nhị phân (.bin):** Sử dụng định dạng nhị phân tự định nghĩa thông qua `BinaryService` để lưu trạng thái các đối tượng và nạp lại để vẽ tiếp.
- [x] **Xuất ảnh (PNG):** Hỗ trợ xuất vùng vẽ ra định dạng ảnh PNG. 
  - *Đặc biệt:* Tích hợp thuật toán tự động quét **Bounding Box** để ảnh xuất ra vừa khít với các đối tượng đã vẽ, loại bỏ khoảng trắng thừa của giấy.

### 🚀 Tính năng nâng cao (Tối ưu trải nghiệm)
- [x] **Zoom & Pan:** Hỗ trợ phóng to/thu nhỏ (Pinch gesture / Mouse wheel) và di chuyển tờ giấy (Panning) vô hạn.

---
