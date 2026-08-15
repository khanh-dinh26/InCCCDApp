using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace InCCCDApp
{
    public static class ImageHelper
    {
        // Kích thước chuẩn thẻ CCCD theo tỉ lệ ID-1 (85.6mm x 53.98mm)
        // Độ phân giải 1024 x 646 vừa nét căng khi in ấn, vừa nhẹ máy cho Core i3 RAM 4GB
        public const int TARGET_WIDTH = 1024;
        public const int TARGET_HEIGHT = 646;

        public static Bitmap CropAndFixPerspective(Bitmap srcBitmap, List<System.Drawing.Point> points)
        {
            if (srcBitmap == null) return null;
            if (points == null || points.Count != 4)
                return (Bitmap)srcBitmap.Clone();

            // 1. TỰ ĐỘNG SẮP XẾP 4 GÓC THEO THỨ TỰ:
            // [0]: Trên-Trái, [1]: Trên-Phải, [2]: Dưới-Phải, [3]: Dưới-Trái
            Point2f[] srcPoints = OrderPoints(points);

            // 2. 4 góc đích phẳng chuẩn hình chữ nhật
            Point2f[] dstPoints = new Point2f[]
            {
                new Point2f(0, 0),
                new Point2f(TARGET_WIDTH, 0),
                new Point2f(TARGET_WIDTH, TARGET_HEIGHT),
                new Point2f(0, TARGET_HEIGHT)
            };

            // Dùng using để giải phóng Mat của OpenCV ngay lập tức, chống tràn RAM
            using (Mat srcMat = BitmapConverter.ToMat(srcBitmap))
            using (Mat dstMat = new Mat())
            using (Mat matrix = Cv2.GetPerspectiveTransform(srcPoints, dstPoints))
            {
                Cv2.WarpPerspective(srcMat, dstMat, matrix, new OpenCvSharp.Size(TARGET_WIDTH, TARGET_HEIGHT), InterpolationFlags.Cubic);
                return BitmapConverter.ToBitmap(dstMat);
            }
        }

        /// <summary>
        /// Thuật toán sắp xếp 4 góc ngẫu nhiên quanh trọng tâm:
        /// Tự động đưa về: Top-Left -> Top-Right -> Bottom-Right -> Bottom-Left
        /// Bất chấp người dùng bấm chuột theo thứ tự nào.
        /// </summary>
        private static Point2f[] OrderPoints(List<System.Drawing.Point> pts)
        {
            // 1. Tính trọng tâm (Centroid) của 4 điểm
            float cx = (float)pts.Average(p => p.X);
            float cy = (float)pts.Average(p => p.Y);

            // 2. Sắp xếp 4 điểm theo góc quay quanh trọng tâm (theo chiều kim đồng hồ)
            var sortedClockwise = pts
                .Select(p => new Point2f(p.X, p.Y))
                .OrderBy(p => Math.Atan2(p.Y - cy, p.X - cx))
                .ToArray();

            // 3. Tìm điểm Top-Left (điểm có tổng x + y nhỏ nhất trong 4 điểm)
            int tlIndex = 0;
            float minSum = float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                float sum = sortedClockwise[i].X + sortedClockwise[i].Y;
                if (sum < minSum)
                {
                    minSum = sum;
                    tlIndex = i;
                }
            }

            // 4. Xếp lại mảng bắt đầu từ Top-Left và đi theo chiều kim đồng hồ
            Point2f[] ordered = new Point2f[4];
            for (int i = 0; i < 4; i++)
            {
                ordered[i] = sortedClockwise[(tlIndex + i) % 4];
            }

            // ordered[0]: Top-Left
            // ordered[1]: Top-Right
            // ordered[2]: Bottom-Right
            // ordered[3]: Bottom-Left
            return ordered;
        }

        // Tương thích ngược: chuyển đổi tọa độ chuột khi PictureBox đặt ở chế độ Zoom thông thường
        public static System.Drawing.Point TranslateZoomMousePosition(PictureBox pic, System.Drawing.Point coordinates)
        {
            if (pic.Image == null) return coordinates;

            float imgRatio = (float)pic.Image.Width / pic.Image.Height;
            float boxRatio = (float)pic.Width / pic.Height;

            int realW, realH, xOffset = 0, yOffset = 0;

            if (imgRatio >= boxRatio)
            {
                realW = pic.Width;
                realH = (int)(pic.Width / imgRatio);
                yOffset = (pic.Height - realH) / 2;
            }
            else
            {
                realH = pic.Height;
                realW = (int)(pic.Height * imgRatio);
                xOffset = (pic.Width - realW) / 2;
            }

            int adjX = coordinates.X - xOffset;
            int adjY = coordinates.Y - yOffset;

            if (adjX < 0 || adjX > realW || adjY < 0 || adjY > realH)
                return System.Drawing.Point.Empty;

            float scale = (float)pic.Image.Width / realW;
            return new System.Drawing.Point((int)(adjX * scale), (int)(adjY * scale));
        }

        public static System.Drawing.Point ConvertToDisplayPoint(PictureBox pic, System.Drawing.Point realPt)
        {
            if (pic.Image == null) return realPt;

            float imgRatio = (float)pic.Image.Width / pic.Image.Height;
            float boxRatio = (float)pic.Width / pic.Height;

            int realW, realH, xOffset = 0, yOffset = 0;
            if (imgRatio >= boxRatio)
            {
                realW = pic.Width;
                realH = (int)(pic.Width / imgRatio);
                yOffset = (pic.Height - realH) / 2;
            }
            else
            {
                realH = pic.Height;
                realW = (int)(pic.Height * imgRatio);
                xOffset = (pic.Width - realW) / 2;
            }

            float scale = (float)realW / pic.Image.Width;
            return new System.Drawing.Point((int)(realPt.X * scale) + xOffset, (int)(realPt.Y * scale) + yOffset);
        }
    }
}