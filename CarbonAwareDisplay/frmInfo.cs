using Microsoft.Toolkit.Uwp.Notifications;
using System.Configuration;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Management;
using System.Runtime.InteropServices;

namespace LightMeasure
{
    public partial class frmInfo : Form
    {
        // Medium Consumptiion Watts per minute per inche
        double cLCD = 0.050448718;
        double cOLED = 0.031944444;
        double cPlasma = 0.097179487;
        double cCRT = 0.098484848;

        // Fator Reduce Bright
        double fBright = 0.820416667;

        // Max Reduction Consumption by Color at Average Brightness 
        double fColorReducationMax = 0.64;
        double fColorReducationMedium = 0.24;
        double fColorReducationMin = 0.13;

        // Time Online (minute x hour x days year)
        double fTimeOnYear = 60 * 24 * 365;

        // Local Variables
        private static List<Location> Local = null;
        private double EmissionRegionLbs = 0;
        private double EmissionGrWmin = 0;
        private static List<dtScreen> lScreen = new List<dtScreen>();
        private const int tmrReloadScreen = 60;
        private frmStart fStart = new frmStart();
        private bool notificationFirst = true;
        public frmInfo()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            tmrReload.Interval = tmrReloadScreen * 1000;
            trbTela1.Value = Auxiliar.GetCurrentBrightness();
            trbTela2.Value = 100;
            trbTela3.Value = 100;
            (double lat, double lng) = Auxiliar.GetPosition();
            Local = WattTime.GetRegion(lat, lng);
            PopulateComboLocation();
            DefineMonitorType();
            DefineNotification();
        }

        private void DefineNotification()
        {
            try
            {
                if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["Notification"]))
                    chkNotification.Checked = Convert.ToBoolean(ConfigurationManager.AppSettings["Notification"]);
            }
            catch { }
        }

        #region Events
        private void tmrReload_Tick(object sender, EventArgs e)
        {
            if (DateTime.Now.Minute % 10 == 0)
            {
                LoadEmission();
            }
            ExecuteScreenCalculate();
            if (notificationFirst || DateTime.Now.Minute == 0)
            {
                if (lScreen != null && lScreen.Count > 0)
                {
                    notificationFirst = false;
                    ShowNotification();
                }
            }
        }

        private void ShowNotification()
        {
            if (chkNotification.Checked)
                try
                {
                    var colorReduction = (lScreen.Sum(_ => _.ReductionColorShow) / Screen.AllScreens.Count() * 100).ToString("N2");
                    var saveCo2 = (lScreen.Sum(_ => _.SaveEmissionYear) / 1000).ToString("N3");
                    var saveTree = (lScreen.Sum(_ => _.SaveEmissionYear) / 1000 / 10).ToString("N1");
                    new ToastContentBuilder()
                        .AddArgument("action", "viewConversation")
                        .AddArgument("conversationId", 9813)
                        .AddText($"You're saving CO2e emission {saveCo2} kg/y")
                        .AddText($"Because your optimized color reduction is {colorReduction}%!")
                        .AddText($"It's like planting {saveTree} trees per year")
                        .Show();
                }
                catch
                {

                }

        }

        private void tmrLeft_Tick(object sender, EventArgs e)
        {
            lbTimerLeft.Text = (Convert.ToInt16(lbTimerLeft.Text) - 1).ToString();
        }

        private void cmbRegion_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            if (cmbRegion.SelectedItem != null)
            {
                LoadEmission();
                ExecuteScreenCalculate();
                pnlSelectRegion.Visible = false;
                pBoxBack.Visible = true;
            }
            this.Cursor = Cursors.Default;
        }

        private void LoadEmission()
        {
            var sBa = Local.Find(_ => _.name == cmbRegion.SelectedItem.ToString()).code;
            Auxiliar.AddUpdateAppSettings("MyRegion", Convert.ToString(cmbRegion.SelectedItem));
            EmissionRegionLbs = WattTime.GetRealEmission(sBa);
            EmissionGrWmin = EmissionRegionLbs * 0.45359237 / 1000 / 60;
            lkRegion.Text = Convert.ToInt32(EmissionGrWmin * 1000 * 60).ToString("N0") + "g/kWh";
        }

        private void trbTela1_ValueChanged(object sender, EventArgs e)
        {
            lblTela1Bright.Text = trbTela1.Value.ToString() + "%";
        }

        private void trbTela2_ValueChanged(object sender, EventArgs e)
        {
            lblTela2Bright.Text = trbTela2.Value.ToString() + "%";
        }

        private void trbTela3_ValueChanged(object sender, EventArgs e)
        {
            lblTela3Bright.Text = trbTela3.Value.ToString() + "%";
        }

        private void trbTela1_MouseCaptureChanged(object sender, EventArgs e)
        {
            RecalculateBright(trbTela1, lblTela1BasicCO2Emit, lblTela1ActualCO2Emit, lblTela1SaveCO2EmitYear, lblTela1SaveEmission, lblTela1Trees, lblTela1SaveMore);
        }

        private void trbTela2_MouseCaptureChanged(object sender, EventArgs e)
        {
            RecalculateBright(trbTela2, lblTela2BasicCO2Emit, lblTela2ActualCO2Emit, lblTela2SaveCO2EmitYear, lblTela2SaveEmission, lblTela2Trees, lblTela2SaveMore);
        }

        private void trbTela3_MouseCaptureChanged(object sender, EventArgs e)
        {
            RecalculateBright(trbTela3, lblTela3BasicCO2Emit, lblTela3ActualCO2Emit, lblTela3SaveCO2EmitYear, lblTela3SaveEmission, lblTela3Trees, lblTela3SaveMore);
        }

        private void cmbTela2_SelectedValueChanged(object sender, EventArgs e)
        {
            Auxiliar.AddUpdateAppSettings("tpMonitor2", Convert.ToString(cmbTela2.SelectedItem));
            RecalculateType(cmbTela2, lblTela2BasicCO2Emit, lblTela2ActualCO2Emit, lblTela2SaveCO2EmitYear, lblTela2SaveEmission, lblTela2Trees, lblTela2SaveMore);
        }

        private void cmbTela1_SelectedValueChanged(object sender, EventArgs e)
        {
            Auxiliar.AddUpdateAppSettings("tpMonitor1", Convert.ToString(cmbTela1.SelectedItem));
            RecalculateType(cmbTela1, lblTela1BasicCO2Emit, lblTela1ActualCO2Emit, lblTela1SaveCO2EmitYear, lblTela1SaveEmission, lblTela1Trees, lblTela1SaveMore);
        }

        private void cmbTela3_SelectedValueChanged(object sender, EventArgs e)
        {
            Auxiliar.AddUpdateAppSettings("tpMonitor3", Convert.ToString(cmbTela3.SelectedItem));
            RecalculateType(cmbTela3, lblTela3BasicCO2Emit, lblTela3ActualCO2Emit, lblTela3SaveCO2EmitYear, lblTela3SaveEmission, lblTela3Trees, lblTela3SaveMore);
        }

        private void pBoxBack_Click(object sender, EventArgs e)
        {
            fStart.Show();
            this.Hide();
        }

        private void frmInfo_Shown(object sender, EventArgs e)
        {
            fStart.Owner = this;
            if (cmbRegion.SelectedItem != null)
            {
                fStart.Show();
                this.Hide();
            }
            else
            {
                pnlSelectRegion.Visible = true;
            }
        }

        #endregion

        #region Methods
        private void PopulateComboLocation()
        {
            cmbRegion.Text = "Selected Region";
            lkRegion.Text = "";
            cmbRegion.Items.Clear();
            foreach (var l in Local)
            {
                cmbRegion.Items.Add(l.name);
            }
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["MyRegion"]))
                cmbRegion.SelectedItem = ConfigurationManager.AppSettings["MyRegion"];
            else if (Local.Count == 1)
                cmbRegion.SelectedIndex = 0;
        }

        private void DefineMonitorType()
        {
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["tpMonitor1"]))
                cmbTela1.SelectedItem = ConfigurationManager.AppSettings["tpMonitor1"];
            else
                cmbTela1.SelectedItem = "LCD";
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["tpMonitor2"]))
                cmbTela2.SelectedItem = ConfigurationManager.AppSettings["tpMonitor2"];
            else
                cmbTela2.SelectedItem = "LCD";
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["tpMonitor3"]))
                cmbTela3.SelectedItem = ConfigurationManager.AppSettings["tpMonitor3"];
            else
                cmbTela3.SelectedItem = "LCD";
        }

        private void ExecuteScreenCalculate()
        {
            lblTelas.Text = Screen.AllScreens.Count().ToString();
            byte im = 1;

            foreach (var sc in Screen.AllScreens)
            {
                var acScreen = lScreen.Find(_ => _.ScreenName == sc.DeviceName);
                if (acScreen == null)
                    acScreen = new dtScreen { ScreenName = sc.DeviceName };

                Rectangle rect;
                uint dpix, dpiy;
                Color csc;
                GetInfoScreen.GetColorScreen(sc, out rect, out dpix, out dpiy, out csc);
                acScreen.X = rect.X;
                acScreen.Y = rect.Y;
                acScreen.Width = rect.Width;
                acScreen.Height = rect.Height;
                acScreen.dpiX = dpix;
                acScreen.dpiY = dpiy;
                acScreen.R = csc.R;
                acScreen.G = csc.G;
                acScreen.B = csc.B;
                acScreen.Diagonal = Math.Sqrt(sc.Bounds.Width * sc.Bounds.Width / dpix + sc.Bounds.Height * sc.Bounds.Height / dpiy) / 10;

                var sTela = "";
                if (im == 1)
                {
                    lblTela1.Tag = sc.DeviceName;
                    trbTela1.Tag = sc.DeviceName;
                    cmbTela1.Tag = sc.DeviceName;
                    sTela = cmbTela1.Text;
                    pnlTela1.Visible = true;
                    lblTela1Size.Text = acScreen.Width.ToString() + " x " + acScreen.Height.ToString() + " (dpi " + acScreen.dpiX.ToString() + ") " + acScreen.Diagonal.ToString("N1") + "\"";
                    acScreen.Brightness = trbTela1.Value;
                }
                else if (im == 2)
                {
                    lblTela2.Tag = sc.DeviceName;
                    trbTela2.Tag = sc.DeviceName;
                    cmbTela2.Tag = sc.DeviceName;
                    sTela = cmbTela2.Text;
                    pnlTela2.Visible = true;
                    lblTela2Size.Text = acScreen.Width.ToString() + " x " + acScreen.Height.ToString() + " (dpi " + acScreen.dpiX.ToString() + ") " + acScreen.Diagonal.ToString("N1") + "\"";
                    acScreen.Brightness = trbTela2.Value;
                }
                else if (im == 3)
                {
                    lblTela3.Tag = sc.DeviceName;
                    trbTela3.Tag = sc.DeviceName;
                    cmbTela3.Tag = sc.DeviceName;
                    sTela = cmbTela3.Text;
                    pnlTela3.Visible = true;
                    lblTela3Size.Text = acScreen.Width.ToString() + " x " + acScreen.Height.ToString() + " (dpi " + acScreen.dpiX.ToString() + ") " + acScreen.Diagonal.ToString("N1") + "\"";
                    acScreen.Brightness = trbTela3.Value;
                }
                acScreen.ScreenType = sTela == "LCD/LED" ? dtScreen.mnType.LCDLED : sTela == "OLED" ? dtScreen.mnType.OLED : sTela == "Plasma" ? dtScreen.mnType.Plasma : dtScreen.mnType.CRT;
                acScreen.ConsumeMediumType = sTela == "LCD/LED" ? cLCD : sTela == "OLED" ? cOLED : sTela == "Plasma" ? cPlasma : cCRT;

                // Calculate Save
                acScreen = CalculateSave(acScreen);
                if (im == 1)
                {
                    PopulateDados(acScreen, lblTela1BasicCO2Emit, lblTela1ActualCO2Emit, lblTela1SaveCO2EmitYear, lblTela1SaveEmission, lblTela1Trees, lblTela1SaveMore);
                }
                else if (im == 2)
                {
                    PopulateDados(acScreen, lblTela2BasicCO2Emit, lblTela2ActualCO2Emit, lblTela2SaveCO2EmitYear, lblTela2SaveEmission, lblTela2Trees, lblTela2SaveMore);
                }
                else if (im == 3)
                {
                    PopulateDados(acScreen, lblTela3BasicCO2Emit, lblTela3ActualCO2Emit, lblTela3SaveCO2EmitYear, lblTela3SaveEmission, lblTela3Trees, lblTela3SaveMore);
                }
                im++;
                UpdateScreen(acScreen);
            }
            PopulateTotal();
            lbTimerLeft.Text = tmrReloadScreen.ToString();
            tmrLeft.Start();
            tmrReload.Start();
        }

        private void UpdateScreen(dtScreen acScreen)
        {
            if (lScreen.Find(_ => _.ScreenName == acScreen.ScreenName) != null)
                lScreen.Remove(lScreen.Find(_ => _.ScreenName == acScreen.ScreenName));
            lScreen.Add(acScreen);
        }

        private void PopulateTotal()
        {
            pnlTotal.Visible = true;
            lblTotalBasicCO2Emit.Text = "Regular CO2e Emit " + (lScreen.Sum(_ => _.BasicCO2Emit)).ToString("N4") + " g/m";
            lblTotalActualCO2Emit.Text = (lScreen.Sum(_ => _.FinalCO2Emit)).ToString("N4");
            lblTotalSaveCO2EmitYear.Text = (lScreen.Sum(_ => _.SaveEmissionYear) /1000).ToString("N3");
            lblTotalSaveEmission.Text = (lScreen.Sum(_ => _.ReductionColorShow) / Screen.AllScreens.Count() * 100).ToString("N2") + "%";
            lblTotalTrees.Text = ((lScreen.Sum(_ => _.SaveEmissionYear) / 1000) / 10).ToString("N1");
            lblTotalSaveMore.Text = ((lScreen.Sum(_ => _.FinalCO2Emit) - lScreen.Sum(_ => _.PossibleCO2Emit)) /1000 * fTimeOnYear / 10).ToString("N1");
            lblTotalSaveMore.Visible = ((lScreen.Sum(_ => _.FinalCO2Emit) - lScreen.Sum(_ => _.PossibleCO2Emit)) / 1000 * fTimeOnYear / 10) > 0;
            lblTotalSaveMoreText.Visible = lblTotalSaveMore.Visible;
            fStart.lbPctReducion.Text = (lScreen.Sum(_ => _.ReductionColorShow) / Screen.AllScreens.Count() * 100).ToString("N2");
            fStart.lbCO2SaveEmission.Text = (lScreen.Sum(_ => _.SaveEmissionYear) / 1000).ToString("N3");
            fStart.lbTreeSave.Text = ((lScreen.Sum(_ => _.SaveEmissionYear) / 1000) / 10).ToString("N1");
            if (lScreen.Sum(_ => _.ReductionColorShow) / Screen.AllScreens.Count() * 100 == 0)
            {

                lblTotalSaveEmission.Text = "N/A";
                fStart.lbPctReducion.Text = "N/A";
            }

        }

        private void PopulateDados(dtScreen acScreen,
                                    Label lbTelaBasicCO2Emit,
                                    Label lbTelaActualCO2Emit,
                                    Label lbTelaSaveCO2EmitYear,
                                    Label lbTelaSaveEmission,
                                    Label lbTelaTrees,
                                    Label lbTelaSaveMore)
        {
            lbTelaBasicCO2Emit.Text = "Regular CO2e Emission " + (acScreen.BasicCO2Emit).ToString("N4") + " g/m";
            lbTelaActualCO2Emit.Text = (acScreen.FinalCO2Emit).ToString("N4");
            lbTelaSaveCO2EmitYear.Text = (acScreen.SaveEmissionYear / 1000).ToString("N3");
            lbTelaSaveEmission.Text = (acScreen.ReductionColorShow * 100).ToString("N2") + "%";
            lbTelaTrees.Text = ((acScreen.SaveEmissionYear / 1000) / 10 ).ToString("N1");
            lbTelaSaveMore.Text = (((acScreen.FinalCO2Emit - acScreen.PossibleCO2Emit) / 1000 / 10) * fTimeOnYear ).ToString("N1");
            lbTelaSaveMore.Visible = (((acScreen.FinalCO2Emit - acScreen.PossibleCO2Emit)/ 1000) * fTimeOnYear ) > 0;
            lblTela1SaveMoreText.Visible = lbTelaSaveMore.Visible;
            if (acScreen.ReductionColorShow * 100 == 0)
            {
                lbTelaSaveEmission.Text = "N/A";
            }
        }
        private dtScreen CalculateSave(dtScreen acScreen)
        {
            acScreen.ColorReducationBright = (acScreen.Brightness <= 25 ? fColorReducationMin : acScreen.Brightness <= 60 ? fColorReducationMedium : fColorReducationMax);
            acScreen.ScreenConsumeWatts = (acScreen.Diagonal * acScreen.ConsumeMediumType);
            acScreen.ReductionColorShow = 1 - ((acScreen.R + acScreen.G + acScreen.B) / 765.0);
            // LCD/LED Don´t reflect Color Reduction
            if (acScreen.ScreenType == dtScreen.mnType.LCDLED)
                acScreen.ReductionColorShow = 0;
            acScreen.ReducationColorFinal = acScreen.ColorReducationBright * acScreen.ReductionColorShow;
            acScreen.BasicCO2Emit = EmissionGrWmin * acScreen.ScreenConsumeWatts;
            acScreen.ReductionBright = (100 - acScreen.Brightness) * fBright / 100;
            acScreen.ActualCO2Emit = acScreen.BasicCO2Emit - (acScreen.BasicCO2Emit * acScreen.ReductionBright);
            acScreen.FinalCO2Emit = acScreen.ActualCO2Emit - (acScreen.ActualCO2Emit * acScreen.ReducationColorFinal);
            acScreen.PossibleCO2Emit = acScreen.ActualCO2Emit - (acScreen.ActualCO2Emit * acScreen.ColorReducationBright);
            acScreen.SaveEmissionYear = ((acScreen.BasicCO2Emit - acScreen.FinalCO2Emit) * fTimeOnYear);
            return acScreen;
        }


        private void RecalculateType(ComboBox cmbTela,
                                    Label lbTelaBasicCO2Emit,
                                    Label lbTelaActualCO2Emit,
                                    Label lbTelaSaveCO2EmitYear,
                                    Label lbTelaSaveEmission,
                                    Label lbTelaTrees,
                                    Label lbTelaSaveMore)
        {
            dtScreen acScreen = lScreen.Find(_ => _.ScreenName == cmbTela.Tag);
            if (acScreen != null)
            {
                acScreen.ScreenType = cmbTela.SelectedItem == "LCD/LED" ? dtScreen.mnType.LCDLED : cmbTela.SelectedItem == "OLED" ? dtScreen.mnType.OLED : cmbTela.SelectedItem == "Plasma" ? dtScreen.mnType.Plasma : dtScreen.mnType.CRT;
                acScreen.ConsumeMediumType = cmbTela.SelectedItem == "LCD/LED" ? cLCD : cmbTela.SelectedItem == "OLED" ? cOLED : cmbTela.SelectedItem == "Plasma" ? cPlasma : cCRT;
                acScreen = CalculateSave(acScreen);
                PopulateDados(acScreen, lbTelaBasicCO2Emit, lbTelaActualCO2Emit, lbTelaSaveCO2EmitYear, lbTelaSaveEmission, lbTelaTrees, lbTelaSaveMore);
                UpdateScreen(acScreen);
                PopulateTotal();
            }
        }

        private void RecalculateBright(TrackBar trbTela,
                                    Label lbTelaBasicCO2Emit,
                                    Label lbTelaActualCO2Emit,
                                    Label lbTelaSaveCO2EmitYear,
                                    Label lbTelaSaveEmission,
                                    Label lbTelaTrees,
                                    Label lbTelaSaveMore)
        {
            dtScreen acScreen = lScreen.Find(_ => _.ScreenName == trbTela.Tag);
            acScreen.Brightness = trbTela.Value;
            acScreen = CalculateSave(acScreen);
            PopulateDados(acScreen, lbTelaBasicCO2Emit, lbTelaActualCO2Emit, lbTelaSaveCO2EmitYear, lbTelaSaveEmission, lbTelaTrees, lbTelaSaveMore);
            UpdateScreen(acScreen);
            PopulateTotal();
        }
        #endregion

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private void chkNotification_CheckedChanged(object sender, EventArgs e)
        {
            Auxiliar.AddUpdateAppSettings("Notification", Convert.ToString(chkNotification.Checked));
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            var fAbout = new frmAbout();
            fAbout.Show();
        }
    }

    #region Object
    public class dtScreen
    {
        public string? ScreenName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public uint dpiX { get; set; }
        public uint dpiY { get; set; }
        public double Diagonal { get; set; }
        public double Brightness { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public double ConsumeMediumType { get; set; }
        public mnType ScreenType { get; set; }

        public double BasicCO2Emit { get; set; }
        public double ActualCO2Emit { get; set; }
        public double FinalCO2Emit { get; set; }
        public double SaveEmissionYear { get; set; }
        public double ReductionColorShow { get; set; }
        public double PossibleCO2Emit { get; set; }
        public double ColorReducationBright { get; set; }
        public double ScreenConsumeWatts { get; set; }
        public double ReducationColorFinal { get; set; }
        public double ReductionBright { get; set; }

        public enum mnType
        {
            LCDLED,
            OLED,
            Plasma,
            CRT
        }
    }

    #endregion

}