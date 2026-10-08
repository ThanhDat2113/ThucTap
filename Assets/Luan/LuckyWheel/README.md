# Lucky Wheel UI (modular)

- `Sprites/WheelBase_Modular.png`: mâm 12 ô trống, không chứa item hay chữ.
- `Sprites/ModularItems`: 12 icon mới có nền trong suốt.
- `Sprites/WheelPointer_Modular.png`: kim chỉ đứng yên khi mâm quay.
- `SpinCenterButton.png`: nút QUAY ở tâm, không xoay theo mâm.
- `LuckyWheelController`: danh sách phần thưởng theo chiều kim đồng hồ, bắt đầu từ ô trên cùng.

## Thay item

1. Chọn `LuckyWheelCanvas/Decorations/WheelArea` trong Hierarchy.
2. Trong component `LuckyWheelController`, mở danh sách `Prizes`.
3. Thay `Id`, `Display Name`, `Icon`, `Amount` hoặc `Weight`.
4. Mở menu ba chấm của component và chọn `Rebuild Item Views` để cập nhật hình trong Scene.

`Weight` là trọng số xác suất. Ví dụ item A có weight 10 và item B có weight 5 thì A có xác suất gấp đôi B.

Nút tâm và `QUAY 1` quay một lần; `QUAY 10` chạy mười lượt liên tiếp. Sự kiện `On Prize Won (String)` trả về `id` của phần thưởng sau mỗi lượt để nối với Inventory/Reward Manager.

## Sửa giá kim cương trên giao diện

Chọn `LuckyWheelCanvas/RightPanel`, mở component `Lucky Wheel Purchase UI` rồi nhập `Price For One`, `Price For Ten` và `Discount Percent`. Hai giá kim cương và badge giảm giá là TextMeshPro riêng, cập nhật ngay trong Scene View. Giá mặc định là 50 và 400 kim cương; 400 tương ứng tiết kiệm 20% so với mười lượt giá 50. Các giá này hiện chỉ điều khiển phần hiển thị, chưa trừ kim cương của tài khoản.
