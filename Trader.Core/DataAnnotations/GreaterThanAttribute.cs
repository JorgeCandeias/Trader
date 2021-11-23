using System.Reflection;

namespace System.ComponentModel.DataAnnotations
{
    public sealed class GreaterThanAttribute : CompareAttribute
    {
        public GreaterThanAttribute(string otherProperty) : base(otherProperty)
        {
            ErrorMessage = "'{0}' must be greater than '{1}'";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (validationContext is null) throw new ArgumentNullException(nameof(validationContext));

            var info = validationContext.ObjectType.GetProperty(OtherProperty);
            if (info is null)
            {
                throw new ValidationException($"The type '{validationContext.ObjectType.FullName}' does not have a property named '{OtherProperty}'");
            }

            var comparerType = typeof(Comparer<>).MakeGenericType(info.PropertyType);
            if (comparerType is null)
            {
                throw new ValidationException($"Cannot generate comparer type for type '{info.PropertyType.FullName}'");
            }

            var comparerProperty = comparerType.GetProperty(nameof(Comparer<object>.Default), BindingFlags.Public | Reflection.BindingFlags.Static);
            if (comparerProperty is null)
            {
                throw new ValidationException($"Cannot find property '{nameof(Comparer<object>.Default)}' of the comparer class for type '{info.PropertyType.FullName}'");
            }

            var comparerInstance = comparerProperty.GetValue(null);
            if (comparerInstance is null)
            {
                throw new ValidationException($"Cannot access default comparer instance for type '{info.PropertyType.FullName}'");
            }

            var comparerMethod = comparerType.GetMethod(nameof(Comparer<object>.Compare), BindingFlags.Public | BindingFlags.Instance, new[] { info.PropertyType, info.PropertyType });
            if (comparerMethod is null)
            {
                throw new ValidationException($"Cannot access comparer method of comparer type '{comparerType.FullName}'");
            }

            var otherValue = info.GetValue(validationContext.ObjectInstance, null);
            var result = comparerMethod.Invoke(comparerInstance, new[] { value, otherValue });
            if (result is null)
            {
                throw new ValidationException($"Could not get comparison result from comparer type '{comparerType.FullName}'");
            }

            if (result is not int comparison)
            {
                throw new ValidationException($"Could not interpret comparison result '{result}'");
            }

            if (comparison <= 0)
            {
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
            }

            return ValidationResult.Success;
        }
    }
}