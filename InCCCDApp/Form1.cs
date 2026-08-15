using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace InCCCDApp
{
    public partial class Form1 : Form
    {
        private const int CARDS_PER_PAGE = 4;

        // Kích thước thẻ CCCD chuẩn (85.6 x 54 mm) quy ra point (1 pt = 1/72 inch)
        private const float CARD_W_PT = 242.6f;
        private const float CARD_H_PT = 153.0f;
        private const float CARD_GAP_PT = 40f;   // khoảng hở dọc giữa 2 thẻ (~14 mm) cho dễ cắt

        // UI Controls
        private GroupBox grpTruoc, grpSau, grpInAn;
        private Button btnChonTruoc, btnXoayTruoc, btnResetTruoc;
        private Button btnChonSau, btnXoaySau, btnResetSau;
        private PictureBox picTruoc, picSau;
        private Button btnLuuThem, btnXoaHet, btnXuatWord;
        private ListView lvDanhSach;
        private Label lblHuongDan, lblSoLuong;

        // Ảnh gốc & Ảnh preview đã nắn
        private Bitmap bmpTruocGoc = null;
        private Bitmap bmpSauGoc = null;
        private List<System.Drawing.Point> ptsTruoc = new List<System.Drawing.Point>();
        private List<System.Drawing.Point> ptsSau = new List<System.Drawing.Point>();
        private Bitmap bmpTruocDaCat = null;
        private Bitmap bmpSauDaCat = null;

        // Biến điều khiển Zoom & Pan cho Mặt Trước
        private float zoomTruoc = 1.0f;
        private PointF panTruoc = new PointF(0, 0);
        private bool isPanningTruoc = false;
        private System.Drawing.Point lastMouseTruoc;

        // Biến điều khiển Zoom & Pan cho Mặt Sau
        private float zoomSau = 1.0f;
        private PointF panSau = new PointF(0, 0);
        private bool isPanningSau = false;
        private System.Drawing.Point lastMouseSau;

        // Danh sách thẻ chờ in
        private List<Tuple<Bitmap, Bitmap>> danhSachCCCD = new List<Tuple<Bitmap, Bitmap>>();

        public Form1()
        {
            InitAutoUI();
            BindEvents();
        }

        private void InitAutoUI()
        {
            this.Text = "Phần mềm In CCCD 2 Mặt Đối Xứng Chuẩn Kích Thước";
            this.Size = new Size(1180, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new System.Drawing.Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.BackColor = Color.FromArgb(246, 247, 249);

            try
            {
                this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
            }

            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            TableLayoutPanel tlpRoot = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));

            TableLayoutPanel tlpMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(4)
            };
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            //  1. MẶT TRƯỚC  
            grpTruoc = new GroupBox { Text = "MẶT TRƯỚC (Giữ Ctrl + Lăn chuột để Phóng to, Giữ chuột phải để Kéo)", Dock = DockStyle.Fill, Padding = new Padding(8, 16, 8, 8), Margin = new Padding(6) };
            Panel pnlHeaderTruoc = new Panel { Dock = DockStyle.Top, Height = 42 };
            btnChonTruoc = new Button { Text = "1. CHỌN ẢNH", Bounds = new Rectangle(4, 3, 115, 34), BackColor = Color.FromArgb(179, 217, 255), FlatStyle = FlatStyle.Flat };
            btnXoayTruoc = new Button { Text = "Xoay Ảnh", Bounds = new Rectangle(126, 3, 90, 34), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnResetTruoc = new Button { Text = "Xóa / Làm Lại", Bounds = new Rectangle(222, 3, 115, 34), BackColor = Color.FromArgb(250, 150, 150), FlatStyle = FlatStyle.Flat };
            pnlHeaderTruoc.Controls.AddRange(new Control[] { btnChonTruoc, btnXoayTruoc, btnResetTruoc });

            picTruoc = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(225, 225, 225),
                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                Cursor = Cursors.Cross
            };
            grpTruoc.Controls.Add(picTruoc);
            grpTruoc.Controls.Add(pnlHeaderTruoc);

            //   2. MẶT SAU  
            grpSau = new GroupBox { Text = "MẶT SAU (Giữ Ctrl + Lăn chuột để Phóng to, Giữ chuột phải để Kéo)", Dock = DockStyle.Fill, Padding = new Padding(8, 16, 8, 8), Margin = new Padding(6) };
            Panel pnlHeaderSau = new Panel { Dock = DockStyle.Top, Height = 42 };
            btnChonSau = new Button { Text = "1. CHỌN ẢNH", Bounds = new Rectangle(4, 3, 115, 34), BackColor = Color.FromArgb(179, 217, 255), FlatStyle = FlatStyle.Flat };
            btnXoaySau = new Button { Text = "Xoay Ảnh", Bounds = new Rectangle(126, 3, 90, 34), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnResetSau = new Button { Text = "Xóa / Làm Lại", Bounds = new Rectangle(222, 3, 115, 34), BackColor = Color.FromArgb(250, 150, 150), FlatStyle = FlatStyle.Flat };
            pnlHeaderSau.Controls.AddRange(new Control[] { btnChonSau, btnXoaySau, btnResetSau });

            picSau = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(225, 225, 225),
                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                Cursor = Cursors.Cross
            };
            grpSau.Controls.Add(picSau);
            grpSau.Controls.Add(pnlHeaderSau);

            tlpMain.Controls.Add(grpTruoc, 0, 0);
            tlpMain.Controls.Add(grpSau, 1, 0);

            //   3. KHU VỰC IN ẤN CĂN GIỮA  
            grpInAn = new GroupBox { Text = "IN ẤN & DANH SÁCH CHỜ (4 thẻ / tờ A4 - In dọc 1 cột, quá 4 thẻ sẽ tự sang tờ mới)", Dock = DockStyle.Fill, Margin = new Padding(6) };

            lblHuongDan = new Label
            {
                Text = "* Hướng dẫn: Click 4 góc thẻ để nắn thẳng. Khi ảnh nhỏ, giữ phím Ctrl + Lăn chuột để phóng to.",
                ForeColor = Color.DarkSlateBlue,
                Location = new Point(20, 20),
                AutoSize = true
            };

            lblSoLuong = new Label
            {
                Text = "Đã chọn: 0 thẻ",
                Font = new System.Drawing.Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.DarkRed,
                Location = new Point(340, 20),
                AutoSize = true
            };

            int startX = Math.Max(10, (1168 - 1040) / 2);
            Panel pnlCenterBottom = new Panel
            {
                Size = new Size(1040, 160),
                Location = new Point(startX, 48),
                BackColor = Color.Transparent
            };

            lvDanhSach = new ListView
            {
                Location = new Point(0, 0),
                Size = new Size(420, 160),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            lvDanhSach.Columns.Add("STT", 45);
            lvDanhSach.Columns.Add("Mặt thẻ", 150);
            lvDanhSach.Columns.Add("Tờ in", 75);
            lvDanhSach.Columns.Add("Trạng thái", 130);

            Panel pnlActions = new Panel
            {
                Location = new Point(440, 0),
                Size = new Size(170, 160)
            };

            btnLuuThem = new Button
            {
                Text = "+ LƯU THẺ NÀY\nVÀO BẢN IN",
                Dock = DockStyle.Top,
                Height = 105,
                BackColor = Color.FromArgb(255, 220, 120),
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            // Nút "XÓA HẾT TẤT CẢ" thay thế nút xóa thẻ chọn
            btnXoaHet = new Button
            {
                Text = "Xóa hết tất cả",
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.FromArgb(255, 220, 220),
                ForeColor = Color.DarkRed,
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            pnlActions.Controls.Add(btnLuuThem);
            pnlActions.Controls.Add(btnXoaHet);

            btnXuatWord = new Button
            {
                Location = new Point(630, 0),
                Size = new Size(410, 160),
                Text = "XUẤT WORD MỞ IN NGAY\n\n(Chuẩn 8.56 x 5.4 cm)\n4 thẻ dọc / trang - 2 Mặt Đối Xứng",
                BackColor = Color.FromArgb(130, 225, 130),
                FlatStyle = FlatStyle.Flat,
                Font = new System.Drawing.Font("Segoe UI", 13.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            pnlCenterBottom.Controls.AddRange(new Control[] {
                lvDanhSach,
                pnlActions,
                btnXuatWord
            });

            grpInAn.Controls.AddRange(new Control[] {
                lblHuongDan, lblSoLuong,
                pnlCenterBottom
            });

            tlpRoot.Controls.Add(tlpMain, 0, 0);
            tlpRoot.Controls.Add(grpInAn, 0, 1);
            this.Controls.Add(tlpRoot);
        }

        private void BindEvents()
        {
            btnChonTruoc.Click += (s, e) => LoadFile(ref bmpTruocGoc, ref bmpTruocDaCat, picTruoc, ptsTruoc, ref zoomTruoc, ref panTruoc);
            btnChonSau.Click += (s, e) => LoadFile(ref bmpSauGoc, ref bmpSauDaCat, picSau, ptsSau, ref zoomSau, ref panSau);

            btnXoayTruoc.Click += (s, e) => Rotate(ref bmpTruocGoc, ref bmpTruocDaCat, picTruoc, ptsTruoc);
            btnXoaySau.Click += (s, e) => Rotate(ref bmpSauGoc, ref bmpSauDaCat, picSau, ptsSau);

            btnResetTruoc.Click += (s, e) => ResetMat(ref bmpTruocGoc, ref bmpTruocDaCat, picTruoc, ptsTruoc, ref zoomTruoc, ref panTruoc);
            btnResetSau.Click += (s, e) => ResetMat(ref bmpSauGoc, ref bmpSauDaCat, picSau, ptsSau, ref zoomSau, ref panSau);

            picTruoc.Paint += (s, e) => DrawCanvas(e.Graphics, picTruoc, bmpTruocDaCat ?? bmpTruocGoc, ptsTruoc, bmpTruocDaCat, zoomTruoc, panTruoc);
            picSau.Paint += (s, e) => DrawCanvas(e.Graphics, picSau, bmpSauDaCat ?? bmpSauGoc, ptsSau, bmpSauDaCat, zoomSau, panSau);

            picTruoc.MouseWheel += (s, e) => HandleMouseWheel(e, picTruoc, ref zoomTruoc, ref panTruoc);
            picSau.MouseWheel += (s, e) => HandleMouseWheel(e, picSau, ref zoomSau, ref panSau);

            picTruoc.MouseDown += (s, e) => HandleMouseDown(e, ref isPanningTruoc, ref lastMouseTruoc);
            picTruoc.MouseMove += (s, e) => HandleMouseMove(e, picTruoc, isPanningTruoc, ref lastMouseTruoc, ref panTruoc);
            picTruoc.MouseUp += (s, e) => HandleMouseUp(e, picTruoc, ref isPanningTruoc, ptsTruoc, bmpTruocGoc, ref bmpTruocDaCat, zoomTruoc, panTruoc);

            picSau.MouseDown += (s, e) => HandleMouseDown(e, ref isPanningSau, ref lastMouseSau);
            picSau.MouseMove += (s, e) => HandleMouseMove(e, picSau, isPanningSau, ref lastMouseSau, ref panSau);
            picSau.MouseUp += (s, e) => HandleMouseUp(e, picSau, ref isPanningSau, ptsSau, bmpSauGoc, ref bmpSauDaCat, zoomSau, panSau);

            picTruoc.MouseEnter += (s, e) => picTruoc.Focus();
            picSau.MouseEnter += (s, e) => picSau.Focus();

            btnLuuThem.Click += BtnLuuThem_Click;
            btnXoaHet.Click += BtnXoaHet_Click;
            btnXuatWord.Click += BtnXuatWord_Click;
        }

        private void LoadFile(ref Bitmap bmpGoc, ref Bitmap bmpCat, PictureBox pic, List<System.Drawing.Point> pts, ref float zoom, ref PointF pan)
        {
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Ảnh CCCD (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    bmpGoc?.Dispose();
                    bmpCat?.Dispose();
                    bmpCat = null;

                    byte[] bytes = File.ReadAllBytes(ofd.FileName);
                    using (MemoryStream ms = new MemoryStream(bytes))
                    using (System.Drawing.Image tempImg = System.Drawing.Image.FromStream(ms))
                    {
                        FixExifOrientation(tempImg);
                        bmpGoc = new Bitmap(tempImg);
                    }

                    pts.Clear();
                    zoom = 1.0f;
                    pan = new PointF(0, 0);
                    pic.Invalidate();
                }
            }
        }

        private void FixExifOrientation(System.Drawing.Image img)
        {
            const int ExifOrientationId = 0x0112;
            if (Array.IndexOf(img.PropertyIdList, ExifOrientationId) > -1)
            {
                var prop = img.GetPropertyItem(ExifOrientationId);
                int val = BitConverter.ToUInt16(prop.Value, 0);
                RotateFlipType flip = RotateFlipType.RotateNoneFlipNone;

                if (val == 3) flip = RotateFlipType.Rotate180FlipNone;
                else if (val == 6) flip = RotateFlipType.Rotate90FlipNone;
                else if (val == 8) flip = RotateFlipType.Rotate270FlipNone;

                if (flip != RotateFlipType.RotateNoneFlipNone)
                {
                    img.RotateFlip(flip);
                    img.RemovePropertyItem(ExifOrientationId);
                }
            }
        }

        private void Rotate(ref Bitmap bmpGoc, ref Bitmap bmpCat, PictureBox pic, List<System.Drawing.Point> pts)
        {
            if (bmpGoc == null) return;
            bmpGoc.RotateFlip(RotateFlipType.Rotate90FlipNone);
            bmpCat?.Dispose();
            bmpCat = null;
            pts.Clear();
            pic.Invalidate();
        }

        private void ResetMat(ref Bitmap bmpGoc, ref Bitmap bmpCat, PictureBox pic, List<System.Drawing.Point> pts, ref float zoom, ref PointF pan)
        {
            if (bmpGoc == null) return;
            bmpCat?.Dispose();
            bmpCat = null;
            pts.Clear();
            zoom = 1.0f;
            pan = new PointF(0, 0);
            pic.Invalidate();
        }

        private void HandleMouseWheel(MouseEventArgs e, PictureBox pic, ref float zoom, ref PointF pan)
        {
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                float oldZoom = zoom;
                if (e.Delta > 0)
                    zoom = Math.Min(zoom * 1.25f, 6.0f);
                else
                    zoom = Math.Max(zoom / 1.25f, 1.0f);

                if (zoom == 1.0f)
                {
                    pan = new PointF(0, 0);
                }
                else
                {
                    pan.X = e.X - (e.X - pan.X) * (zoom / oldZoom);
                    pan.Y = e.Y - (e.Y - pan.Y) * (zoom / oldZoom);
                }
                pic.Invalidate();
            }
        }

        private void HandleMouseDown(MouseEventArgs e, ref bool isPanning, ref System.Drawing.Point lastMouse)
        {
            if (e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle)
            {
                isPanning = true;
                lastMouse = e.Location;
            }
        }

        private void HandleMouseMove(MouseEventArgs e, PictureBox pic, bool isPanning, ref System.Drawing.Point lastMouse, ref PointF pan)
        {
            if (isPanning)
            {
                pan.X += (e.X - lastMouse.X);
                pan.Y += (e.Y - lastMouse.Y);
                lastMouse = e.Location;
                pic.Invalidate();
            }
        }

        private void HandleMouseUp(MouseEventArgs e, PictureBox pic, ref bool isPanning, List<System.Drawing.Point> pts, Bitmap bmpGoc, ref Bitmap bmpCat, float zoom, PointF pan)
        {
            if (e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle)
            {
                isPanning = false;
                return;
            }

            if (e.Button == MouseButtons.Left && bmpGoc != null && bmpCat == null)
            {
                System.Drawing.Point imgPixel = ScreenToImagePoint(pic, e.Location, bmpGoc, zoom, pan);
                if (imgPixel.X >= 0 && imgPixel.X <= bmpGoc.Width && imgPixel.Y >= 0 && imgPixel.Y <= bmpGoc.Height)
                {
                    if (pts.Count < 4)
                    {
                        pts.Add(imgPixel);
                        pic.Invalidate();

                        if (pts.Count == 4)
                        {
                            bmpCat = ImageHelper.CropAndFixPerspective(bmpGoc, pts);
                            pic.Invalidate();
                        }
                    }
                }
            }
        }

        private RectangleF GetBaseImageRect(PictureBox pic, Bitmap bmp)
        {
            if (bmp == null) return RectangleF.Empty;

            float imgRatio = (float)bmp.Width / bmp.Height;
            float boxRatio = (float)pic.Width / pic.Height;

            float w, h, x, y;
            if (imgRatio >= boxRatio)
            {
                w = pic.Width;
                h = pic.Width / imgRatio;
                x = 0;
                y = (pic.Height - h) / 2f;
            }
            else
            {
                h = pic.Height;
                w = pic.Height * imgRatio;
                y = 0;
                x = (pic.Width - w) / 2f;
            }
            return new RectangleF(x, y, w, h);
        }

        private System.Drawing.Point ScreenToImagePoint(PictureBox pic, System.Drawing.Point screenPt, Bitmap bmp, float zoom, PointF pan)
        {
            RectangleF baseRect = GetBaseImageRect(pic, bmp);
            float curX = baseRect.X * zoom + pan.X;
            float curY = baseRect.Y * zoom + pan.Y;
            float curW = baseRect.Width * zoom;
            float curH = baseRect.Height * zoom;

            float relX = (screenPt.X - curX) / curW;
            float relY = (screenPt.Y - curY) / curH;

            return new System.Drawing.Point((int)(relX * bmp.Width), (int)(relY * bmp.Height));
        }

        private PointF ImageToScreenPoint(PictureBox pic, System.Drawing.Point imgPt, Bitmap bmp, float zoom, PointF pan)
        {
            RectangleF baseRect = GetBaseImageRect(pic, bmp);
            float curX = baseRect.X * zoom + pan.X;
            float curY = baseRect.Y * zoom + pan.Y;
            float curW = baseRect.Width * zoom;
            float curH = baseRect.Height * zoom;

            float sx = curX + ((float)imgPt.X / bmp.Width) * curW;
            float sy = curY + ((float)imgPt.Y / bmp.Height) * curH;
            return new PointF(sx, sy);
        }

        private void DrawCanvas(Graphics g, PictureBox pic, Bitmap bmp, List<System.Drawing.Point> pts, Bitmap bmpCat, float zoom, PointF pan)
        {
            if (bmp == null) return;

            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            if (bmpCat != null)
            {
                RectangleF baseRect = GetBaseImageRect(pic, bmpCat);
                g.DrawImage(bmpCat, baseRect);
            }
            else
            {
                RectangleF baseRect = GetBaseImageRect(pic, bmp);
                float curX = baseRect.X * zoom + pan.X;
                float curY = baseRect.Y * zoom + pan.Y;
                float curW = baseRect.Width * zoom;
                float curH = baseRect.Height * zoom;

                g.DrawImage(bmp, curX, curY, curW, curH);

                using (Brush bRed = new SolidBrush(Color.Red))
                using (System.Drawing.Font f = new System.Drawing.Font("Arial", 9.5f, FontStyle.Bold))
                {
                    for (int i = 0; i < pts.Count; i++)
                    {
                        PointF dispPt = ImageToScreenPoint(pic, pts[i], bmp, zoom, pan);
                        g.FillEllipse(bRed, dispPt.X - 6, dispPt.Y - 6, 12, 12);
                        g.DrawString((i + 1).ToString(), f, Brushes.Yellow, dispPt.X + 7, dispPt.Y - 7);
                    }
                }
            }
        }

        private void BtnLuuThem_Click(object sender, EventArgs e)
        {
            if (bmpTruocGoc == null)
            {
                MessageBox.Show("Vui lòng nạp ít nhất mặt trước của thẻ!", "Thông báo");
                return;
            }

            Bitmap finalTruoc = bmpTruocDaCat != null
                ? (Bitmap)bmpTruocDaCat.Clone()
                : new Bitmap(bmpTruocGoc, ImageHelper.TARGET_WIDTH, ImageHelper.TARGET_HEIGHT);

            Bitmap finalSau = null;
            if (bmpSauGoc != null)
            {
                finalSau = bmpSauDaCat != null
                    ? (Bitmap)bmpSauDaCat.Clone()
                    : new Bitmap(bmpSauGoc, ImageHelper.TARGET_WIDTH, ImageHelper.TARGET_HEIGHT);
            }

            danhSachCCCD.Add(new Tuple<Bitmap, Bitmap>(finalTruoc, finalSau));
            ReloadListView();

            ResetCurrentView();
            GC.Collect();
        }

        private void ResetCurrentView()
        {
            ptsTruoc.Clear(); ptsSau.Clear();
            zoomTruoc = 1.0f; panTruoc = new PointF(0, 0);
            zoomSau = 1.0f; panSau = new PointF(0, 0);

            bmpTruocGoc?.Dispose(); bmpTruocGoc = null;
            bmpTruocDaCat?.Dispose(); bmpTruocDaCat = null;
            picTruoc.Invalidate();

            bmpSauGoc?.Dispose(); bmpSauGoc = null;
            bmpSauDaCat?.Dispose(); bmpSauDaCat = null;
            picSau.Invalidate();
        }

        //    == SỰ KIỆN XÓA HẾT DANH SÁCH    ==
        private void BtnXoaHet_Click(object sender, EventArgs e)
        {
            if (danhSachCCCD.Count == 0)
            {
                MessageBox.Show("Danh sách in hiện đang trống!", "Thông báo");
                return;
            }

            var dr = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa toàn bộ {danhSachCCCD.Count} thẻ trong danh sách in không?",
                "Xác nhận xóa hết",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (dr == DialogResult.Yes)
            {
                // Giải phóng toàn bộ bộ nhớ của tất cả các ảnh đã lưu
                for (int i = 0; i < danhSachCCCD.Count; i++)
                {
                    danhSachCCCD[i].Item1?.Dispose();
                    danhSachCCCD[i].Item2?.Dispose();
                }

                danhSachCCCD.Clear();
                ReloadListView();
                GC.Collect();
            }
        }

        private void ReloadListView()
        {
            lvDanhSach.Items.Clear();
            for (int i = 0; i < danhSachCCCD.Count; i++)
            {
                var item = new ListViewItem((i + 1).ToString());
                string info = danhSachCCCD[i].Item2 != null ? "2 Mặt (Trước + Sau)" : "1 Mặt (Trước)";
                int page = i / CARDS_PER_PAGE + 1;
                int slot = i % CARDS_PER_PAGE + 1;
                item.SubItems.Add(info);
                item.SubItems.Add($"Tờ {page} - Ô {slot}");
                item.SubItems.Add("Sẵn sàng in");
                lvDanhSach.Items.Add(item);
            }

            int sheetCount = (danhSachCCCD.Count + CARDS_PER_PAGE - 1) / CARDS_PER_PAGE;
            lblSoLuong.Text = $"Đã chọn: {danhSachCCCD.Count} thẻ - {sheetCount} tờ A4";
        }

        private void BtnXuatWord_Click(object sender, EventArgs e)
        {
            if (danhSachCCCD.Count == 0)
            {
                MessageBox.Show("Danh sách chờ in đang trống! Hãy nạp ít nhất 1 CCCD.", "Thông báo");
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "Word Document (*.docx)|*.docx", FileName = "In_CCCD_2_Mat.docx" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    this.Cursor = Cursors.WaitCursor;
                    try
                    {
                        ExportDuplexWordDoc(sfd.FileName);
                        Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi xuất file Word: " + ex.Message);
                    }
                    finally
                    {
                        this.Cursor = Cursors.Default;
                    }
                }
            }
        }

        private void ExportDuplexWordDoc(string filePath)
        {
            using (DocX doc = DocX.Create(filePath))
            {
                doc.MarginLeft = 28f;
                doc.MarginRight = 28f;
                doc.MarginTop = 35f;
                doc.MarginBottom = 26f;

                int total = danhSachCCCD.Count;
                int pageCount = (total + CARDS_PER_PAGE - 1) / CARDS_PER_PAGE;

                bool hasAnyBack = false;
                for (int i = 0; i < total; i++)
                {
                    if (danhSachCCCD[i].Item2 != null) { hasAnyBack = true; break; }
                }

                // Mỗi tờ gồm 2 trang: trang lẻ = MẶT TRƯỚC, trang chẵn = MẶT SAU.
                // Quá 4 thẻ thì tự động sang tờ mới (trang 3, 4, ...).
                for (int page = 0; page < pageCount; page++)
                {
                    int firstIdx = page * CARDS_PER_PAGE;
                    int lastIdx = Math.Min(firstIdx + CARDS_PER_PAGE - 1, total - 1);

                    // TRANG LẺ: MẶT TRƯỚC - xếp dọc 1 cột sát lề TRÁI, chừa trống bên phải
                    for (int idx = firstIdx; idx <= lastIdx; idx++)
                    {
                        AppendCardParagraph(doc, danhSachCCCD[idx].Item1, Alignment.left, idx == lastIdx);
                    }

                    if (hasAnyBack)
                    {
                        doc.InsertParagraph().InsertPageBreakAfterSelf();

                        // TRANG CHẴN: MẶT SAU - lật gương sang lề PHẢI (giữ nguyên thứ tự trên xuống)
                        // để khi in 2 mặt lật giấy theo cạnh dài, mặt sau khớp đúng vị trí mặt trước.
                        for (int idx = firstIdx; idx <= lastIdx; idx++)
                        {
                            AppendCardParagraph(doc, danhSachCCCD[idx].Item2, Alignment.right, idx == lastIdx);
                        }
                    }

                    // Còn tờ tiếp theo thì ngắt trang
                    if (page < pageCount - 1)
                    {
                        doc.InsertParagraph().InsertPageBreakAfterSelf();
                    }
                }

                doc.Save();
            }
        }

        /// <summary>
        /// Thêm 1 thẻ vào trang Word dưới dạng 1 đoạn văn (1 dòng dọc).
        /// align = Alignment.left (sát lề trái) hoặc Alignment.right (sát lề phải).
        /// </summary>
        private void AppendCardParagraph(DocX doc, Bitmap src, Alignment align, bool isLast)
        {
            Paragraph para = doc.InsertParagraph();
            para.Alignment = align;
            para.SpacingBefore(0);
            if (!isLast) para.SpacingAfter(CARD_GAP_PT);

            // Thẻ thiếu mặt sau: vẫn giữ đúng khoảng trống dọc để các thẻ không bị dồn lên
            if (src == null)
            {
                para.SpacingBefore(CARD_H_PT);
                return;
            }

            using (Bitmap borderedImg = DrawCropBorder(src))
            using (MemoryStream ms = new MemoryStream())
            {
                borderedImg.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                var p = doc.AddImage(ms).CreatePicture();
                p.Width = CARD_W_PT;
                p.Height = CARD_H_PT;
                para.AppendPicture(p);
            }
        }

        private Bitmap DrawCropBorder(Bitmap src)
        {
            Bitmap result = new Bitmap(src);
            using (Graphics g = Graphics.FromImage(result))
            using (Pen cropPen = new Pen(Color.FromArgb(90, 90, 90), 3))
            {
                cropPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                cropPen.DashPattern = new float[] { 5, 3 };
                g.DrawRectangle(cropPen, 1, 1, result.Width - 3, result.Height - 3);
            }
            return result;
        }
    }
}