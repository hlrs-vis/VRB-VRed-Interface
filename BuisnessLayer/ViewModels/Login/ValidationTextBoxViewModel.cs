using System.Text.RegularExpressions;
using BuisnessLayer.ViewModels.Base;

namespace BuisnessLayer.ViewModels.Login
{
    public class ValidationTextBoxViewModel : CommonBase
    {
        private readonly Regex _stringPattern;

        public ValidationTextBoxViewModel(Regex stringPattern = null)
        {
            IsValid = true;
            CheckValidation = false;
            _stringPattern = stringPattern;
        }


        private bool _isValid;
        public bool IsValid
        {
            get => _isValid;
            set
            {
                if (value == _isValid)
                    return;

                _isValid = value;
                RaisePropertyChanged("IsValid");
            }
        }

        private string _value;
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                RaisePropertyChanged("Value");

                Validate();
            }
        }


        private bool _checkValidation;
        public bool CheckValidation
        {
            get => _checkValidation;
            set
            {
                _checkValidation = value;
                RaisePropertyChanged("CheckValidation");
            }
        }


        public void Validate()
        {
            // check if the value is null or Empty
            IsValid = !string.IsNullOrEmpty(Value);

            // value is allready false and NOT valid so return and dont check string pattern
            if (IsValid == false)
                return;

            // no string pattern required so return
            if (_stringPattern == null)
                return;

            // check the string pattern -> for example for an mail address or ip address
            IsValid = _stringPattern.IsMatch(Value);
        }
    }
}