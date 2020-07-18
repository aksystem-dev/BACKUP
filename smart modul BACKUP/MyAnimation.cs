using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace smart_modul_PRINT
{
    //public class MyAnimation : DoubleAnimation
    //{
    //    public double Speed
    //    {
    //        get { return (double)GetValue(SpeedProperty); }
    //        set { SetValue(SpeedProperty, value); }
    //    }

    //    public static readonly DependencyProperty SpeedProperty =
    //        DependencyProperty.Register("Speed", typeof(double), typeof(MyAnimation), new PropertyMetadata(0));


    //    protected override Freezable CreateInstanceCore()
    //    {
    //        return new MyAnimation();
    //    }

    //    protected override double GetCurrentValueCore(double defaultOriginValue, double defaultDestinationValue, AnimationClock animationClock)
    //    {
    //        var start = From ?? defaultOriginValue;
    //        var end = To ?? defaultDestinationValue;

    //        var val = start + Math.Sign(end - start) * Speed * animationClock.CurrentTime.Value.TotalSeconds;

    //             if (val > end) val = end; 
    //        else if (val < start) val = start;
           
    //        return val;
    //    }
    //}
}
