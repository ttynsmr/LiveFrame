
namespace LiveFrame
{
    partial class LiveForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.labelLiveFrame = new System.Windows.Forms.Label();
            this.labelBeRightBack = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labelLiveFrame
            // 
            this.labelLiveFrame.BackColor = System.Drawing.Color.Transparent;
            this.labelLiveFrame.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelLiveFrame.Enabled = false;
            this.labelLiveFrame.Font = new System.Drawing.Font("MS UI Gothic", 90F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelLiveFrame.Location = new System.Drawing.Point(0, 0);
            this.labelLiveFrame.Name = "labelLiveFrame";
            this.labelLiveFrame.Size = new System.Drawing.Size(800, 450);
            this.labelLiveFrame.TabIndex = 0;
            this.labelLiveFrame.Text = "LiveFrame";
            this.labelLiveFrame.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelLiveFrame.UseMnemonic = false;
            // 
            // labelBeRightBack
            // 
            this.labelBeRightBack.BackColor = System.Drawing.Color.Transparent;
            this.labelBeRightBack.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelBeRightBack.Enabled = false;
            this.labelBeRightBack.Font = new System.Drawing.Font("MS UI Gothic", 90F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.labelBeRightBack.Location = new System.Drawing.Point(0, 0);
            this.labelBeRightBack.Name = "labelBeRightBack";
            this.labelBeRightBack.Size = new System.Drawing.Size(800, 450);
            this.labelBeRightBack.TabIndex = 1;
            this.labelBeRightBack.Text = "be right back";
            this.labelBeRightBack.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelBeRightBack.UseMnemonic = false;
            this.labelBeRightBack.Visible = false;
            // 
            // LiveForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.ControlBox = false;
            this.Controls.Add(this.labelBeRightBack);
            this.Controls.Add(this.labelLiveFrame);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LiveForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.LiveForm_Paint);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label labelLiveFrame;
        private System.Windows.Forms.Label labelBeRightBack;
    }
}

