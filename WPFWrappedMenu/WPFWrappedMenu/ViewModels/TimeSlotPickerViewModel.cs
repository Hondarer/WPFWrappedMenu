﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using WPFWrappedMenu.Commands;

namespace WPFWrappedMenu.ViewModels
{
    public class TimeSlotPickerViewModel : BindableBase
    {
        public class TimeSlotViewModel : BindableBase
        {
            public string Description { get; set; }

            public DateTime SpecifyTimeSlotStartTime { get; set; }

            private bool _isSelected = false;

            public bool IsSelected
            {
                get
                {
                    return _isSelected;
                }
                set
                {
                    SetProperty(ref _isSelected, value);
                }
            }
        }

        private DateTime _currentTimeSlotStartTime;

        public DateTime CurrentTimeSlotStartTime

        {
            get
            {
                return _currentTimeSlotStartTime;
            }
            set
            {
                if (SetProperty(ref _currentTimeSlotStartTime, TruncateDateTime(value)) == true)
                {
                    RefreshTimeSlotsViewModel();
                }
            }
        }

        private DateTime? _selectedTimeSlotStartTime;

        public DateTime? SelectedTimeSlotStartTime

        {
            get
            {
                return _selectedTimeSlotStartTime;
            }
            set
            {
                if (SetProperty(ref _selectedTimeSlotStartTime, TruncateDateTime(value)) == true)
                {
                    OnPropertyChanged(nameof(SelectedTimeSlotStartTimeString));
                }
            }
        }

        public string SelectedTimeSlotStartTimeString
        {
            get
            {
                return string.Format("{0:HH:mm}", SelectedTimeSlotStartTime);
            }
            set
            {
                // 入力された文字列を DateTime として評価する
                if (DateTime.TryParseExact(value,
                    Resources.StringResource.StartTimeParseFormats.Split(','),
                    CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime parsedDateTime) == false)
                {
                    // 無効な値の場合にはソースを更新しない
                    // View を元の値に戻すために、PropertyChanged イベントを発行する
                    OnPropertyChanged();
                    return;
                }

                // 有意であれば設定
                ChangeSelectedTimeSlotCore(parsedDateTime);
            }
        }

        private List<TimeSlotViewModel> _timeSpans;

        public List<TimeSlotViewModel> TimeSpans
        {
            get
            {
                return _timeSpans;
            }
            set
            {
                SetProperty(ref _timeSpans, value);
            }
        }

        private bool _popupOpen;

        public bool PopupOpen
        {
            get
            {
                return _popupOpen;
            }
            set
            {
                SetProperty(ref _popupOpen, value);
            }
        }

        private List<TimeSlotViewModel> _shortcuts;

        public List<TimeSlotViewModel> Shortcuts
        {
            get
            {
                return _shortcuts;
            }
            set
            {
                SetProperty(ref _shortcuts, value);
            }
        }

        public DelegateCommand SpecifyTimeSlotCommamnd { get; }

        public DelegateCommand PreviousTimeSlotCommamnd { get; }

        public DelegateCommand NextTimeSlotCommamnd { get; }

        public TimeSlotPickerViewModel()
        {
            SpecifyTimeSlotCommamnd = new DelegateCommand(
                parameter =>
                {
                    if (parameter is DateTime specifyDate)
                    {
                        ChangeSelectedTimeSlotCore(specifyDate);
                    }

                    // ポップアップを閉じる
                    PopupOpen = false;

                    // TODO: 選択(指定)できない条件はあるか
                });

            PreviousTimeSlotCommamnd = new DelegateCommand(
                parameter =>
                {
                    if (parameter is not DateTime specifyTimeSlotStartTime)
                    {
                        specifyTimeSlotStartTime = CurrentTimeSlotStartTime;
                    }
                    // 1/1 00:00 からさかのぼることはできない。翌日基準で 30 分さかのぼることで時間帯を算出する。
                    SpecifyTimeSlotCommamnd.Execute(specifyTimeSlotStartTime.AddDays(1).AddMinutes(-30));
                },
                parameter =>
                {
                    if (parameter is not DateTime specifyTimeSlotStartTime)
                    {
                        specifyTimeSlotStartTime = CurrentTimeSlotStartTime;
                    }
                    // 1/1 00:00 からさかのぼることはできない。翌日基準で 30 分さかのぼることで時間帯を算出する。
                    return SpecifyTimeSlotCommamnd.CanExecute(specifyTimeSlotStartTime.AddDays(1).AddMinutes(-30));
                });

            NextTimeSlotCommamnd = new DelegateCommand(
                parameter =>
                {
                    if (parameter is not DateTime specifyTimeSlotStartTime)
                    {
                        specifyTimeSlotStartTime = CurrentTimeSlotStartTime;
                    }
                    SpecifyTimeSlotCommamnd.Execute(specifyTimeSlotStartTime.AddMinutes(30));
                },
                parameter =>
                {
                    if (parameter is not DateTime specifyTimeSlotStartTime)
                    {
                        specifyTimeSlotStartTime = CurrentTimeSlotStartTime;
                    }
                    return SpecifyTimeSlotCommamnd.CanExecute(specifyTimeSlotStartTime.AddMinutes(30));
                });

            List<TimeSlotViewModel> timeSpans = new List<TimeSlotViewModel>();
            for (int i = 0; i < 48; i++)
            {
                TimeSlotViewModel timeViewModel = new TimeSlotViewModel() { SpecifyTimeSlotStartTime = new DateTime().AddMinutes(30 * i) };
                timeSpans.Add(timeViewModel);
            }

            TimeSpans = timeSpans;

            InvalidateTimeSlot();
        }

        public DateTime? TruncateDateTime(DateTime? dateTime)
        {
            if (dateTime == null)
            {
                return null;
            }

            return TruncateDateTime((DateTime)dateTime);
        }

        public DateTime TruncateDateTime(DateTime dateTime)
        {
            // 日付情報があれば削除する
            DateTime timeOfDay = new DateTime().Add(dateTime.TimeOfDay);

            // 30 分単位で切り捨てる
            return timeOfDay.AddTicks(-(timeOfDay.Ticks % TimeSpan.FromMinutes(30).Ticks));
        }

        public void CurrentDateTimeChanged(object sender, EventArgs e)
        {
            InvalidateTimeSlot();
        }

        private void InvalidateTimeSlot()
        {
            CurrentTimeSlotStartTime = DateTimeManager.Instance.CurrentDateTime;
        }

        private void ChangeSelectedTimeSlotCore(DateTime? specifyDate)
        {
            SelectedTimeSlotStartTime = specifyDate;

            foreach (object vm in TimeSpans.Union(Shortcuts))
            {
                if (vm is TimeSlotViewModel dateViewModel)
                {
                    dateViewModel.IsSelected = dateViewModel.SpecifyTimeSlotStartTime == SelectedTimeSlotStartTime;
                }
            }
        }

        private void RefreshTimeSlotsViewModel()
        {
            #region ショートカットの作成

            List<TimeSlotViewModel> shortcuts = new List<TimeSlotViewModel>()
            {
                new TimeSlotViewModel(){SpecifyTimeSlotStartTime=CurrentTimeSlotStartTime.AddMinutes(30), Description="next of current timeslot" },
                new TimeSlotViewModel(){SpecifyTimeSlotStartTime=CurrentTimeSlotStartTime, Description="current timeslot" },
                // 1/1 00:00 からさかのぼることはできない。翌日基準で 30 分さかのぼることで時間帯を算出する。
                new TimeSlotViewModel(){SpecifyTimeSlotStartTime=CurrentTimeSlotStartTime.AddDays(1).AddMinutes(-30), Description="previous of current timeslot" }
            };

            Shortcuts = shortcuts;

            #endregion

            // 選択状態の更新
            ChangeSelectedTimeSlotCore(SelectedTimeSlotStartTime);

            // コマンドの実行可否再評価を依頼
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
