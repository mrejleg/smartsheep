namespace Project.Core.Exceptions
{
    public class MismatchException : Exception
    {
        public string PropertyName { get; set; }
        public object ValueInRoute { get; set; }
        public object ValueInForm { get; set; }

        public MismatchException(string propertyName, object valueInRoute, object valueInForm, Exception innerException)
            : base($"{propertyName} value in the Route [{valueInRoute}] does not match {propertyName} value in the Form [{valueInForm}]", innerException)
        {
            PropertyName = propertyName;
            ValueInRoute = valueInRoute;
            ValueInForm = valueInForm;
        }

        public MismatchException(string propertyName, object valueInRoute, object valueInForm)
            : base($"{propertyName} value in the Route [{valueInRoute}] does not match {propertyName} value in the Form [{valueInForm}]")
        {
            PropertyName = propertyName;
            ValueInRoute = valueInRoute;
            ValueInForm = valueInForm;
        }
    }
}