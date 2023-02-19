﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Client.Controls;
using Client.Envir;
using Client.Models;
using Client.UserModels;
using Library;
using Library.SystemModels;

namespace Client.Scenes.Views
{
    public sealed class InventoryDialog : DXImageControl
    {
        #region Properties

        public DXItemGrid Grid;

        public DXLabel TitleLabel, PrimaryCurrencyLabel, SecondaryCurrencyLabel, WeightLabel, WalletLabel, PrimaryCurrencyTitle, SecondaryCurrencyTitle;
        public DXButton CloseButton, SortButton, TrashButton;

        #region PrimaryCurrency

        public CurrencyInfo PrimaryCurrency
        {
            get => _PrimaryCurrency;
            set
            {
                if (_PrimaryCurrency == value) return;

                CurrencyInfo oldValue = _PrimaryCurrency;
                _PrimaryCurrency = value;

                OnPrimaryCurrencyChanged(oldValue, value);
            }
        }
        private CurrencyInfo _PrimaryCurrency;

        public event EventHandler<EventArgs> PrimaryCurrencyChanged;
        public void OnPrimaryCurrencyChanged(CurrencyInfo oValue, CurrencyInfo nValue)
        {
            if (GameScene.Game.User == null)
                return;

            foreach (DXItemCell cell in Grid.Grid)
                cell.Selected = false;

            RefreshPrimaryCurrency();

            PrimaryCurrencyChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region SecondaryCurrency

        public CurrencyInfo SecondaryCurrency
        {
            get => _SecondaryCurrency;
            set
            {
                if (_SecondaryCurrency == value) return;

                CurrencyInfo oldValue = _SecondaryCurrency;
                _SecondaryCurrency = value;

                OnPrimaryCurrencyChanged(oldValue, value);
            }
        }
        private CurrencyInfo _SecondaryCurrency;

        public event EventHandler<EventArgs> SecondaryCurrencyChanged;
        public void OnSecondaryCurrencyChanged(CurrencyInfo oValue, CurrencyInfo nValue)
        {
            if (GameScene.Game.User == null)
                return;

            foreach (DXItemCell cell in Grid.Grid)
                cell.Selected = false;

            RefreshSecondaryCurrency();

            SecondaryCurrencyChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        public override void OnIsVisibleChanged(bool oValue, bool nValue)
        {
            if (!IsVisible)
                Grid.ClearLinks();

            if (IsVisible)
                BringToFront();

            if (Settings != null)
                Settings.Visible = nValue;

            base.OnIsVisibleChanged(oValue, nValue);
        }

        public override void OnLocationChanged(Point oValue, Point nValue)
        {
            base.OnLocationChanged(oValue, nValue);

            if (Settings != null && IsMoving)
                Settings.Location = nValue;
        }

        public override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.KeyCode)
            {
                case Keys.Escape:
                    if (CloseButton.Visible)
                    {
                        CloseButton.InvokeMouseClick();
                        if (!Config.EscapeCloseAll)
                            e.Handled = true;
                    }
                    break;
            }
        }

        #endregion

        #region Settings

        public WindowSetting Settings;
        public WindowType Type => WindowType.InventoryBox;

        public void LoadSettings()
        {
            if (Type == WindowType.None || !CEnvir.Loaded) return;

            Settings = CEnvir.WindowSettings.Binding.FirstOrDefault(x => x.Resolution == Config.GameSize && x.Window == Type);

            if (Settings != null)
            {
                ApplySettings();
                return;
            }

            Settings = CEnvir.WindowSettings.CreateNewObject();
            Settings.Resolution = Config.GameSize;
            Settings.Window = Type;
            Settings.Size = Size;
            Settings.Visible = Visible;
            Settings.Location = Location;
        }

        public void ApplySettings()
        {
            if (Settings == null) return;

            Location = Settings.Location;

            Visible = Settings.Visible;
        }


        #endregion

        public InventoryDialog()
        {
            LibraryFile = LibraryFile.Interface;
            Index = 130;
            Movable = true;
            Sort = true;

            CloseButton = new DXButton
            {
                Parent = this,
                Index = 15,
                LibraryFile = LibraryFile.Interface,
            };
            CloseButton.Location = new Point(DisplayArea.Width - CloseButton.Size.Width - 5, 5);
            CloseButton.MouseClick += (o, e) => Visible = false;

            TitleLabel = new DXLabel
            {
                Text = CEnvir.Language.InventoryDialogTitle,
                Parent = this,
                Font = new Font(Config.FontName, CEnvir.FontSize(10F), FontStyle.Bold),
                ForeColour = Color.FromArgb(198, 166, 99),
                Outline = true,
                OutlineColour = Color.Black,
                IsControl = false,
            };
            TitleLabel.Location = new Point((DisplayArea.Width - TitleLabel.Size.Width) / 2, 8);

            Grid = new DXItemGrid
            {
                GridSize = new Size(6, 8),
                Parent = this,
                ItemGrid = GameScene.Game.Inventory,
                GridType = GridType.Inventory,
                Location = new Point(20, 39),
                GridPadding = 1,
                BackColour = Color.Empty,
                Border = false
            };

            CEnvir.LibraryList.TryGetValue(LibraryFile.GameInter, out MirLibrary library);

            DXControl WeightBar = new DXControl
            {
                Parent = this,
                Location = new Point(53, 355),
                Size = library.GetSize(360),
            };
            WeightBar.BeforeDraw += (o, e) =>
            {
                if (library == null) return;

                if (MapObject.User.Stats[Stat.BagWeight] == 0) return;

                float percent = Math.Min(1, Math.Max(0, MapObject.User.BagWeight / (float)MapObject.User.Stats[Stat.BagWeight]));

                if (percent == 0) return;

                MirImage image = library.CreateImage(360, ImageType.Image);

                if (image == null) return;

                PresentTexture(image.Image, this, new Rectangle(WeightBar.DisplayArea.X, WeightBar.DisplayArea.Y, (int)(image.Width * percent), image.Height), Color.White, WeightBar);
            };

            WeightLabel = new DXLabel
            {
                Parent = this,
                ForeColour = Color.White,
                Outline = true,
                OutlineColour = Color.Black,
                DrawFormat = TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter
            };
            WeightLabel.SizeChanged += (o, e) =>
            {
                WeightLabel.Location = new Point(WeightBar.Location.X + (WeightBar.Size.Width - WeightLabel.Size.Width) / 2, WeightBar.Location.Y - 1 + (WeightBar.Size.Height - WeightLabel.Size.Height) / 2);
            };

            PrimaryCurrencyTitle = new DXLabel
            {
                AutoSize = false,
                ForeColour = Color.Goldenrod,
                DrawFormat = TextFormatFlags.VerticalCenter | TextFormatFlags.Left,
                Parent = this,
                Location = new Point(55, 381),
                Font = new Font(Config.FontName, CEnvir.FontSize(8F), FontStyle.Bold),
                Text = CEnvir.Language.InventoryDialogPrimaryCurrencyTitle,
                Size = new Size(97, 20)
            };

            PrimaryCurrencyLabel = new DXLabel
            {
                AutoSize = false,
                ForeColour = Color.White,
                DrawFormat = TextFormatFlags.VerticalCenter | TextFormatFlags.Right,
                Parent = this,
                Location = new Point(80, 381),
                Text = "0",
                Size = new Size(97, 20)
            };
            PrimaryCurrencyLabel.MouseClick += PrimaryCurrencyLabel_MouseClick;

            SecondaryCurrencyTitle = new DXLabel
            {
                AutoSize = false,
                ForeColour = Color.DarkOrange,
                DrawFormat = TextFormatFlags.VerticalCenter | TextFormatFlags.Left,
                Parent = this,
                Location = new Point(55, 400),
                Font = new Font(Config.FontName, CEnvir.FontSize(8F), FontStyle.Bold),
                Text = CEnvir.Language.InventoryDialogSecondaryCurrencyTitle,
                Size = new Size(97, 20)
            };

            SecondaryCurrencyLabel = new DXLabel
            {
                AutoSize = false,
                ForeColour = Color.White,
                DrawFormat = TextFormatFlags.VerticalCenter | TextFormatFlags.Right,
                Parent = this,
                Location = new Point(80, 400),
                Text = "0",
                Size = new Size(97, 20)
            };
            SecondaryCurrencyLabel.MouseClick += SecondaryCurrencyLabel_MouseClick;

            SortButton = new DXButton
            {
                LibraryFile = LibraryFile.GameInter,
                Index = 364,
                Parent = this,
                Location = new Point(180, 384),
                Hint = CEnvir.Language.InventoryDialogSortButtonHint,
                Enabled = false
            };

            TrashButton = new DXButton
            {
                LibraryFile = LibraryFile.GameInter,
                Index = 358,
                Parent = this,
                Location = new Point(218, 384),
                Hint = CEnvir.Language.InventoryDialogTrashButtonHint,
                Enabled = false
            };

            WalletLabel = new DXLabel
            {
                Parent = this,
                Location = new Point(8, 380),
                Hint = CEnvir.Language.InventoryDialogWalletLabelHint,
                Size = new Size(45, 40),
                Sound = SoundIndex.GoldPickUp
            };
            WalletLabel.MouseClick += WalletLabel_MouseClick;
        }


        private void PrimaryCurrencyLabel_MouseClick(object sender, MouseEventArgs e)
        {
            DXSoundManager.Play(SoundIndex.GoldPickUp);

            if (GameScene.Game.SelectedCell == null)
            {
                var userCurrency = GameScene.Game.User.GetCurrency(PrimaryCurrency);

                if (!userCurrency.CanPickup) return;
                DXSoundManager.Play(SoundIndex.GoldPickUp);

                if (GameScene.Game.CurrencyPickedUp == null && userCurrency.Amount > 0)
                    GameScene.Game.CurrencyPickedUp = userCurrency;
                else
                    GameScene.Game.CurrencyPickedUp = null;
            }
        }

        private void SecondaryCurrencyLabel_MouseClick(object sender, MouseEventArgs e)
        {
            if (GameScene.Game.SelectedCell == null)
            {
                var userCurrency = GameScene.Game.User.GetCurrency(SecondaryCurrency);

                if (!userCurrency.CanPickup) return;
                DXSoundManager.Play(SoundIndex.GoldPickUp);

                if (GameScene.Game.CurrencyPickedUp == null && userCurrency.Amount > 0)
                    GameScene.Game.CurrencyPickedUp = userCurrency;
                else
                    GameScene.Game.CurrencyPickedUp = null;
            }
        }

        private void WalletLabel_MouseClick(object sender, MouseEventArgs e)
        {
            GameScene.Game.CurrencyBox.Visible = !GameScene.Game.CurrencyBox.Visible;
        }

        #region Methods

        public void RefreshCurrency()
        {
            RefreshPrimaryCurrency();
            RefreshSecondaryCurrency();
        }

        public void SetPrimaryCurrency(CurrencyInfo currency)
        {
            PrimaryCurrency = currency ?? Globals.CurrencyInfoList.Binding.First(x => x.Type == CurrencyType.Gold);
        }

        private void RefreshPrimaryCurrency()
        {
            SetPrimaryCurrency(PrimaryCurrency);

            var userCurrency = GameScene.Game.User.GetCurrency(PrimaryCurrency);

            PrimaryCurrencyTitle.Text = userCurrency.Info.Abbreviation;
            PrimaryCurrencyLabel.Text = userCurrency.Amount.ToString("#,##0");
        }

        //TODO - Allow secondary currency to change its default??
        private void SetSecondaryCurrency(CurrencyInfo currency)
        {
            SecondaryCurrency = currency ?? Globals.CurrencyInfoList.Binding.First(x => x.Type == CurrencyType.GameGold);
        }

        private void RefreshSecondaryCurrency()
        {
            SetSecondaryCurrency(SecondaryCurrency);

            var userCurrency = GameScene.Game.User.GetCurrency(SecondaryCurrency);

            SecondaryCurrencyTitle.Text = userCurrency.Info.Abbreviation;
            SecondaryCurrencyLabel.Text = userCurrency.Amount.ToString("#,##0");
        }


        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (Grid != null)
                {
                    if (!Grid.IsDisposed)
                        Grid.Dispose();

                    Grid = null;
                }

                if (TitleLabel != null)
                {
                    if (!TitleLabel.IsDisposed)
                        TitleLabel.Dispose();

                    TitleLabel = null;
                }

                if (PrimaryCurrencyLabel != null)
                {
                    if (!PrimaryCurrencyLabel.IsDisposed)
                        PrimaryCurrencyLabel.Dispose();

                    PrimaryCurrencyLabel = null;
                }

                if (SecondaryCurrencyLabel != null)
                {
                    if (!SecondaryCurrencyLabel.IsDisposed)
                        SecondaryCurrencyLabel.Dispose();

                    SecondaryCurrencyLabel = null;
                }

                if (PrimaryCurrencyTitle != null)
                {
                    if (!PrimaryCurrencyTitle.IsDisposed)
                        PrimaryCurrencyTitle.Dispose();

                    PrimaryCurrencyTitle = null;
                }

                if (SecondaryCurrencyTitle != null)
                {
                    if (!SecondaryCurrencyTitle.IsDisposed)
                        SecondaryCurrencyTitle.Dispose();

                    SecondaryCurrencyTitle = null;
                }

                if (WeightLabel != null)
                {
                    if (!WeightLabel.IsDisposed)
                        WeightLabel.Dispose();

                    WeightLabel = null;
                }

                if (WalletLabel != null)
                {
                    if (!WalletLabel.IsDisposed)
                        WalletLabel.Dispose();

                    WalletLabel = null;
                }

                if (CloseButton != null)
                {
                    if (!CloseButton.IsDisposed)
                        CloseButton.Dispose();

                    CloseButton = null;
                }

                if (SortButton != null)
                {
                    if (!SortButton.IsDisposed)
                        SortButton.Dispose();

                    SortButton = null;
                }

                if (TrashButton != null)
                {
                    if (!TrashButton.IsDisposed)
                        TrashButton.Dispose();

                    TrashButton = null;
                }

                if (PrimaryCurrencyTitle != null)
                {
                    if (!PrimaryCurrencyTitle.IsDisposed)
                        PrimaryCurrencyTitle.Dispose();

                    SecondaryCurrencyTitle = null;
                }

                if (SecondaryCurrencyTitle != null)
                {
                    if (!SecondaryCurrencyTitle.IsDisposed)
                        SecondaryCurrencyTitle.Dispose();

                    SecondaryCurrencyTitle = null;
                }
            }
        }

        #endregion
    }
}