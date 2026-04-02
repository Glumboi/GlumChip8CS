using GlumChip8.GUI.Core;
using Microsoft.Win32;
using Raylib_cs;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Wpf.Ui.Controls;
using GalaSoft.MvvmLight.CommandWpf;
using CommunityToolkit.Mvvm.Input;
using GlumChip8Extended.Core;

namespace GlumChip8.GUI.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        nint _wpfParentHandle;
        public nint WPFParentHandle { get => _wpfParentHandle; set => SetProperty(ref _wpfParentHandle, value); }

        private string _currentRomPath = "No Rom loaded!";

        public string CurrentRomPath { get => _currentRomPath; set => SetProperty(ref _currentRomPath, value); }

        EmulatorSettings _emulatorSettings = new();

        public bool IsEmulatorActive => !_raylibHost._pausedEumlation;

        public SymbolRegular PauseRomSymbol
        {
            get
            {
                if (IsEmulatorActive)
                {
                    return SymbolRegular.Pause48;
                }
                else
                {
                    return SymbolRegular.Play48;
                }
            }
        }


        private Chip8RaylibHost _raylibHost = new();
        public Chip8RaylibHost RaylibHost { get => _raylibHost; set => SetProperty(ref _raylibHost, value); }

        private bool _showCollection = true;
        public bool ShowCollection
        {
            get => _showCollection;
            set => SetProperty(ref _showCollection, value);
        }

        public ObservableCollection<KeyValuePair<string, string>> RomCollection { get; set; } = new() { };

        public MainViewModel()
        {
            _emulatorSettings.LoadConfigFile();
            LoadChip8Roms(_emulatorSettings.RomLocation);
            Raylib.SetConfigFlags(ConfigFlags.UndecoratedWindow | ConfigFlags.HiddenWindow);
            Raylib.InitWindow(64 * Display.SCALE, 32 * Display.SCALE, "CHIP-8");
            Raylib.SetTargetFPS(60);
            LoadBusMaster();
        }

        void LoadBusMaster()
        {
            BusMaster.Init(new BusMasterSettings(false, true, false, true));
            // Load window to host
            unsafe
            {
                _raylibHost.UpdateHandle((nint)Raylib.GetWindowHandle());
            }
        }

        void LoadChip8Roms(string load)
        {
            if (!Directory.Exists(load)) return;
            foreach (var item in Directory.GetFiles(load))
            {
                if (Path.GetExtension(item) == ".ch8")
                {
                    KeyValuePair<string, string> keyValuePair = new KeyValuePair<string, string>(Path.GetFileNameWithoutExtension(item), item);
                    RomCollection.Add(keyValuePair);
                }
            }
        }

        [RelayCommand]
        public void Refresh()
        {
            RomCollection.Clear();
            LoadChip8Roms(_emulatorSettings.RomLocation);
        }

        [RelayCommand]
        private void TogglePause()
        {
            _raylibHost.TogglePause();
            OnPropertyChanged(nameof(PauseRomSymbol));
            OnPropertyChanged(nameof(IsEmulatorActive));
        }

        [RelayCommand]
        private void OpenRom()
        {
            var of = new OpenFileDialog();
            of.Title = "Please select the ROM file you want to load!";
            of.Filter = "Chip8 ROM files (*.ch8)|*.ch8|All files (*.*)|*.*";
            of.InitialDirectory = Directory.GetCurrentDirectory();
            if ((bool)of.ShowDialog())
            {
                CurrentRomPath = of.FileName;
                var ext = Path.GetExtension(CurrentRomPath);
                if (String.Equals(ext, ".ch8", StringComparison.OrdinalIgnoreCase) && File.Exists(CurrentRomPath))
                {
                    StartEmulation();
                }
            }
        }

        public void StartEmulation()
        {
            BusMaster.Reset();
            BusMaster.IsInitialized = true;
            BusMaster.GlobalBus.LoadCh8(CurrentRomPath);
            OnPropertyChanged(nameof(RaylibHost));
            OnPropertyChanged(nameof(PauseRomSymbol));
            OnPropertyChanged(nameof(IsEmulatorActive));
            ShowCollection = false;
        }

        [RelayCommand]
        public void Play(KeyValuePair<string, string> selectedRom)
        {
            string romPath = selectedRom.Value;
            CurrentRomPath = romPath;
            StartEmulation();
        }

        [RelayCommand]
        public void CloseRom()
        {
            BusMaster.Reset();
            BusMaster.Running = false;
            OnPropertyChanged(nameof(RaylibHost));
            OnPropertyChanged(nameof(PauseRomSymbol));
            OnPropertyChanged(nameof(IsEmulatorActive));
            ShowCollection = true;
        }

        [RelayCommand]
        public void ResetRom()
        {
            BusMaster.Reset();
        }

        [RelayCommand]
        public void ToggleFps()
        {
            RaylibHost.ToggleFps();
        }
    }

}