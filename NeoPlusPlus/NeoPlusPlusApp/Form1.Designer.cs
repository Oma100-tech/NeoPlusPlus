namespace NeoPlusPlusApp;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1100, 700);
        MinimumSize = new Size(980, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Neo++";
        BackColor = Color.FromArgb(18, 18, 24);
        ForeColor = Color.FromArgb(235, 240, 255);
        Font = new Font("Segoe UI", 10F);
    }
}
