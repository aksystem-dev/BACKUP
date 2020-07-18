using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Hezký přepínač. Funguje stejně jako CheckBox.
    /// </summary>
    public partial class ToggleOnOff : UserControl, INotifyPropertyChanged
    {
        public double CornerRadius => Math.Min(Width, Height) / 2;
        public int OnCol => Orientation == Orientation.Horizontal ? (OnFirst ? 0 : 1) : 0;
        public int OffCol => Orientation == Orientation.Horizontal ? (OnFirst ? 1 : 0) : 0;
        public int OnRow => Orientation == Orientation.Vertical ? (OnFirst ? 0 : 1) : 0;
        public int OffRow => Orientation == Orientation.Vertical ? (OnFirst ? 1 : 0) : 0;
        public int ColSpan => Orientation == Orientation.Horizontal ? 1 : 2;
        public int RowSpan => Orientation == Orientation.Vertical ? 1 : 2;
        public double ToggleDiameter => Math.Min(Width, Height) - 2 * ToggleMargin;

        public double OnOffsetX => (Orientation == Orientation.Horizontal ? Width / 4 : 0) * (OnFirst ? -1 : 1);
        public double OnOffsetY => (Orientation == Orientation.Vertical ? Height / 4 : 0) * (OnFirst ? -1 : 1);
        public double ToggleXTranslate => OnOffsetX * RelXTranslate;
        public double ToggleYTranslate => OnOffsetY * RelYTranslate;
        public Brush CurrentToggleColor => IsToggledOn ? OnColor : OffColor;

        public Orientation Orientation => Width > Height ? Orientation.Horizontal : Orientation.Vertical;

        private Storyboard StoryboardOn;
        private Storyboard StoryboardOff;

        public event EventHandler ToggledOn;
        public event EventHandler ToggledOff;


        public bool IsToggledOn
        {
            get { return (bool)GetValue(IsToggledOnProperty); }
            set
            {
                if (value == IsToggledOn)
                    return;

                SetValue(IsToggledOnProperty, value);
                (value ? ToggledOn : ToggledOff)?.Invoke(this, null);
            }
        }

        public static readonly DependencyProperty IsToggledOnProperty =
            DependencyProperty.Register("IsToggledOn", typeof(bool), typeof(ToggleOnOff), new PropertyMetadata(false));


        public double RelXTranslate
        {
            get { return (double)GetValue(RelXTranslateProperty); }
            set { SetValue(RelXTranslateProperty, value); }
        }

        public static readonly DependencyProperty RelXTranslateProperty =
            DependencyProperty.Register("RelXTranslate", typeof(double), typeof(ToggleOnOff), new PropertyMetadata(0d));


        public double RelYTranslate
        {
            get { return (double)GetValue(RelYTranslateProperty); }
            set { SetValue(RelYTranslateProperty, value); }
        }

        public static readonly DependencyProperty RelYTranslateProperty =
            DependencyProperty.Register("RelYTranslate", typeof(double), typeof(ToggleOnOff), new PropertyMetadata(0d));


        public bool OnFirst
        {
            get { return (bool)GetValue(OnFirstProperty); }
            set { SetValue(OnFirstProperty, value); }
        }

        public static readonly DependencyProperty OnFirstProperty =
            DependencyProperty.Register("OnFirst", typeof(bool), typeof(ToggleOnOff), new PropertyMetadata(true));


        public double ToggleMargin
        {
            get { return (double)GetValue(ToggleMarginProperty); }
            set { SetValue(ToggleMarginProperty, value); }
        }

        public static readonly DependencyProperty ToggleMarginProperty =
            DependencyProperty.Register("ToggleMargin", typeof(double), typeof(ToggleOnOff), new PropertyMetadata(5d));


        public Duration AnimationDuration
        {
            get { return (Duration)GetValue(AnimationDurationProperty); }
            set { SetValue(AnimationDurationProperty, value); }
        }

        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register("AnimationDuration", typeof(Duration), typeof(ToggleOnOff), new PropertyMetadata(new Duration(TimeSpan.Zero)));

        public Brush MouseOverOverlay
        {
            get { return (Brush)GetValue(MouseOverOverlayProperty); }
            set { SetValue(MouseOverOverlayProperty, value); }
        }

        public static readonly DependencyProperty MouseOverOverlayProperty =
            DependencyProperty.Register("MouseOverOverlay", typeof(Brush), typeof(ToggleOnOff),
                new PropertyMetadata(
                    new SolidColorBrush(
                        new Color() { R = 0, G = 0, B = 0, A = 64 }
            )));



        public Brush OnColor
        {
            get { return (Brush)GetValue(OnColorProperty); }
            set { SetValue(OnColorProperty, value); }
        }

        public static readonly DependencyProperty OnColorProperty =
            DependencyProperty.Register("OnColor", typeof(Brush), typeof(ToggleOnOff),
                new PropertyMetadata(new SolidColorBrush(Colors.Lime)));

        public Brush OffColor
        {
            get { return (Brush)GetValue(OffColorProperty); }
            set { SetValue(OffColorProperty, value); }
        }

        public static readonly DependencyProperty OffColorProperty =
            DependencyProperty.Register("OffColor", typeof(Brush), typeof(ToggleOnOff),
                new PropertyMetadata(new SolidColorBrush(Colors.Red)));




        public ToggleOnOff()
        {
            InitializeComponent();

            border.Background = Background;
            Background = null;

            RelXTranslate = IsToggledOn ? 1 : -1;
            RelYTranslate = IsToggledOn ? 1 : -1;

            DoubleAnimation on1 = new DoubleAnimation()
            {
                //From = -1,
                To = 1
            };

            BindingOperations.SetBinding(on1, DoubleAnimation.DurationProperty,
                new Binding(nameof(AnimationDuration)) { Source = this }
            );

            var on2 = on1.Clone();

            DoubleAnimation off1 = new DoubleAnimation()
            {
                //From = 1,
                To = -1
            };

            BindingOperations.SetBinding(off1, DoubleAnimation.DurationProperty,
                new Binding(nameof(AnimationDuration)) { Source = this }
            );

            var off2 = off1.Clone();
            
            StoryboardOn = new Storyboard();
            StoryboardOn.Children.Add(on1);
            StoryboardOn.Children.Add(on2);
            Storyboard.SetTargetProperty(on1, new PropertyPath(RelXTranslateProperty));
            Storyboard.SetTargetProperty(on2, new PropertyPath(RelYTranslateProperty));
            Storyboard.SetTargetName(on1, Name);
            Storyboard.SetTargetName(on2, Name);

            StoryboardOff = new Storyboard();
            StoryboardOff.Children.Add(off1);
            StoryboardOff.Children.Add(off2);
            Storyboard.SetTargetProperty(off1, new PropertyPath(RelXTranslateProperty));
            Storyboard.SetTargetProperty(off2, new PropertyPath(RelYTranslateProperty));
            Storyboard.SetTargetName(off1, Name);
            Storyboard.SetTargetName(off2, Name);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == ToggleOnOff.WidthProperty || e.Property == ToggleOnOff.HeightProperty)
            {
                InvokePropertiesChanged(
                    nameof(CornerRadius),
                    nameof(Orientation),
                    nameof(OnRow),
                    nameof(OffRow),
                    nameof(OnCol),
                    nameof(OffCol),
                    nameof(ColSpan),
                    nameof(RowSpan),
                    nameof(ToggleDiameter),
                    nameof(OnOffsetX),
                    nameof(OnOffsetY),
                    nameof(ToggleXTranslate),
                    nameof(ToggleYTranslate));
            }
            else if (e.Property == ToggleOnOff.RelXTranslateProperty)
            {
                InvokePropertiesChanged(nameof(ToggleXTranslate));
            }
            else if (e.Property == ToggleOnOff.RelYTranslateProperty)
            {
                InvokePropertiesChanged(nameof(ToggleYTranslate));
            }
            else if (e.Property == ToggleOnOff.ToggleMarginProperty)
            {
                InvokePropertiesChanged(nameof(ToggleDiameter));
            }
            else if (e.Property == ToggleOnOff.IsToggledOnProperty)
            {
                InvokePropertiesChanged(nameof(CurrentToggleColor));

                if (IsToggledOn)
                    StoryboardOn.Begin(this);
                else
                    StoryboardOff.Begin(this);
            }
            else if(e.Property == OnColorProperty || e.Property == OffColorProperty)
            {
                InvokePropertiesChanged(nameof(CurrentToggleColor));
            }
            else if (e.Property == ToggleOnOff.IsMouseOverProperty)
            {
                if (border.IsMouseOver)
                    borderInner.Background = MouseOverOverlay;
                else
                    borderInner.Background = null;
            }
        }

        private void InvokePropertiesChanged(params string[] propertyNames)
        {
            if (PropertyChanged == null)
                return;

            foreach (var name in propertyNames)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        private void border_mouseOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

        private void clicked(object sender, MouseButtonEventArgs e)
        {
            IsToggledOn = !IsToggledOn;
        }
    }
}
