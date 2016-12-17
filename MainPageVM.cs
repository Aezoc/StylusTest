using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Core;

namespace StylusTest
{
    #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    class MainPageVM : INotifyPropertyChanged
    {
        private const string DATA_SERVICE_UUID = "0000fff0-0000-1000-8000-00805f9b34fb";
        private const string DATA_IN_CHARACTERISTIC_UUID = "0000fff4-0000-1000-8000-00805f9b34fb";

        public enum StylusButton
        {
            Primary = 2,
            Secondary = 1,
            Both = 3
        }

        public enum StylusButtonStatus
        {
            Press = 1,
            LongPress = 2,
            Release = 3
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<BluetoothLEDevice> devices = new ObservableCollection<BluetoothLEDevice>();
        public ObservableCollection<BluetoothLEDevice> Devices {
            get
            {
                return devices;
            }
            private set
            {
                devices = value;
                OnPropertyChanged("Devices");
            }
        }

        private BluetoothLEDevice selectedDevice = null;
        public BluetoothLEDevice SelectedDevice
        {
            get
            {
                return selectedDevice;
            } set
            {
                if (selectedDevice != value)
                {
                    if (selectedDevice != null)
                        DisconnectStylus();
                    selectedDevice = value;
                    OnPropertyChanged("SelectedDevice");
                    ConnectStylus();
                }
            }
        }

        private string message = "Select a stylus to continue";
        public string Message
        {
            get
            {
                return message;
            }
            set
            {
                if (message != value)
                {
                    message = value;
                    OnPropertyChanged("Message");
                }
            }
        }

        private string rawData = string.Empty;
        public string RawData
        {
            get
            {
                return rawData;
            } set
            {
                if(rawData != value)
                {
                    rawData = value;
                    OnPropertyChanged("RawData");
                }
            }
        }

        public bool CopyToClipboard { get; set; }
            
        public MainPageVM()
        {
            FindBluetoothDevices();
        }

        private async Task FindBluetoothDevices()
        {
            ObservableCollection<BluetoothLEDevice> items = new ObservableCollection<BluetoothLEDevice>();
            foreach (DeviceInformation di in await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector()))
            {
                BluetoothLEDevice bleDevice = await BluetoothLEDevice.FromIdAsync(di.Id);
                items.Add(bleDevice);
            }
            Devices = items;
        }

        private void DisconnectStylus()
        {
            var dataService = SelectedDevice.GetGattService(new Guid(DATA_SERVICE_UUID));
            var dataInCharacteristic = dataService.GetCharacteristics(new Guid(DATA_IN_CHARACTERISTIC_UUID))[0];
            dataInCharacteristic.ValueChanged -= DataInCharacteristic_ValueChanged;
        }

        private void ConnectStylus()
        {
            var dataService = SelectedDevice.GetGattService(new Guid(DATA_SERVICE_UUID));
            var dataInCharacteristic = dataService.GetCharacteristics(new Guid(DATA_IN_CHARACTERISTIC_UUID))[0];
            dataInCharacteristic.ValueChanged += DataInCharacteristic_ValueChanged;
            Message = "Press a stylus button or scan a game object to continue";
        }

        private void DataInCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var response = args.CharacteristicValue.ToArray();
            RawData = string.Join(", ", response);
            if (response.Length == 3)            // Button press or release
            {
                // TODO response[0] == 1 always seems to be true. Figure out why that byte is there.
                var button = (StylusButton)response[1];
                var status = (StylusButtonStatus)response[2];
                Message = string.Format("Stylus button event {0} {1}", button, status);
            }
            else if (response.Length == 4)       // Scan
            {
                // My assumption is that there's no case where we actually need to read this data in whatever
                // format HBS does. As long as we're internally consistent with how we interpret these codes,
                // they're just identifiers that the users will never see or care about.
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(response);
                var value = BitConverter.ToUInt32(response, 0);
                Message = string.Format("Stylus scanned value {0}", value);

                // Put the value on the clipboard to make it easier to store these values in the OpenArcana data source
                if (CopyToClipboard)
                {
                    DataPackage dataPackage = new DataPackage();
                    dataPackage.RequestedOperation = DataPackageOperation.Copy;
                    dataPackage.SetText(value.ToString());
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => Clipboard.SetContent(dataPackage));
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(string.Format("Received an unrecognized {0} length data packet from stylus", response.Length));
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
            {
                OnPropertyChangedUnsafe(propertyName);
            }
            else
            {
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => OnPropertyChangedUnsafe(propertyName));
            }
        }

        private void OnPropertyChangedUnsafe(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
