using IJPSystem.Platform.Domain.Models.Motion;
using CommunityToolkit.Mvvm.ComponentModel; // 또는 기존 사용하던 BindableBase

namespace IJPSystem.Platform.HMI.Models
{
    public partial class AxisItem : ObservableObject
    {
        // 1. 고정된 설정 정보 (Domain 모델)
        public AxisDeviceInfo DeviceInfo { get; set; }

        // 2. 실시간 변하는 상태 정보 (Domain 모델)
        // 전체 객체를 통째로 업데이트하거나 개별 속성을 Notify합니다.
        private AxisStatus? _status;
        public AxisStatus? Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public AxisItem(AxisDeviceInfo info)
        {
            DeviceInfo = info;
            Status = new AxisStatus { AxisNo = info.AxisNo, Name = info.Name };
        }
    }
}