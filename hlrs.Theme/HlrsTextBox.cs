using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using hlrsTheme.Annotations;

namespace hlrsTheme
{
    public class HlrsTextBox : TextBox
    {
        public static readonly DependencyProperty ValidateValueProperty =
            DependencyProperty.Register("ValidateValue", typeof(bool), typeof(HlrsTextBox), new UIPropertyMetadata(null));

        public static readonly DependencyProperty ValidProperty =
            DependencyProperty.Register("Valid", typeof(bool), typeof(HlrsTextBox), new UIPropertyMetadata(OnValidChanged));

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register("Placeholder", typeof(string), typeof(HlrsTextBox), new UIPropertyMetadata(null));

        public static readonly DependencyProperty ValidationBrushProperty =
                    DependencyProperty.Register("ValidationBrush", typeof(Brush), typeof(HlrsTextBox), new UIPropertyMetadata(null));

        public static readonly DependencyProperty ShowValidationTextProperty =
            DependencyProperty.Register("ShowValidationText", typeof(bool), typeof(HlrsTextBox), new UIPropertyMetadata(null));

        public static readonly DependencyProperty ValidationTextProperty =
            DependencyProperty.Register("ValidationText", typeof(string), typeof(HlrsTextBox), new UIPropertyMetadata(null));

        public static readonly DependencyProperty LabelTextProperty =
                DependencyProperty.Register("LabelText", typeof(string), typeof(HlrsTextBox), new UIPropertyMetadata(null));

        public static readonly DependencyProperty ShowLabelProperty =
                    DependencyProperty.Register("ShowLabel", typeof(bool), typeof(HlrsTextBox), new UIPropertyMetadata(null));


        private readonly Brush _standardBorderBrush;

        public HlrsTextBox()
        {
            DefaultStyleKey = typeof(HlrsTextBox);
            GotFocus += OnGotFocus;
            LostFocus += OnLostFocus;
            TextChanged += OnTextChanged;



            _standardBorderBrush = BorderBrush;

        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
	        if (GetTemplateChild("Placeholder") is TextBlock placeHolder) placeHolder.Visibility = Visibility.Collapsed;
			//placeHolder.Visibility = Visibility.Collapsed;
		}

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Text))
            {
                if (GetTemplateChild("Placeholder") is TextBlock placeHolder) placeHolder.Visibility = Visibility.Visible;
                //ShowLabel = false;
            }

            Validate();
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (GetTemplateChild("Placeholder") is TextBlock placeHolder && placeHolder.Visibility == Visibility.Visible)
            {
                ShowLabel = true;
                placeHolder.Visibility = Visibility.Collapsed;
            }

            this.CaretIndex = Text.Length;
        }


        public bool ValidateValue
        {
            get => (bool)GetValue(ValidateValueProperty);
            set => SetValue(ValidateValueProperty, value);
        }

        public bool Valid
        {
            get => (bool)GetValue(ValidProperty);
            set
            {
                SetValue(ValidProperty, value);

                Validate();
            }
        }

        private static void OnValidChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            HlrsTextBox cp = obj as HlrsTextBox;
            cp.Validate();
        }


        private void Validate()
        {
            if (ValidateValue && Valid == false)
            {
                //_standardBorderBrush = BorderBrush;
                // Show Validation
                BorderBrush = ValidationBrush;
                ShowValidationText = true;
            }

            if (ValidateValue && Valid)
            {
                BorderBrush = _standardBorderBrush;
                ShowValidationText = false;
            }
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public Brush ValidationBrush
        {
            get => (Brush)GetValue(ValidationBrushProperty);
            set => SetValue(ValidationBrushProperty, value);
        }

        public bool ShowValidationText
        {
            get => (bool)GetValue(ShowValidationTextProperty);
            set => SetValue(ShowValidationTextProperty, value);
        }

        public string ValidationText
        {
            get => (string)GetValue(ValidationTextProperty);
            set => SetValue(ValidationTextProperty, value);
        }

        public string LabelText
        {
            get => (string)GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }
        public bool ShowLabel
        {
            get => (bool)GetValue(ShowLabelProperty);
            set => SetValue(ShowLabelProperty, value);
        }

      
    }
}
