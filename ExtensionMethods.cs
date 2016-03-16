using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SpeedwayClientWpf
{
    public static class ExtensionMethods
    {
        public static void OnPropertyChanged<T>(this INotifyPropertyChanged notifyPropertyChanged, Expression<Func<T>> memberExpression)
        {
            notifyPropertyChanged.OnPropertyChanged(memberExpression.GetMemberInfo().Name);
        }

        public static MemberInfo GetMemberInfo<T>(this Expression<Func<T>> memberExpression)
        {
            return ((MemberExpression)memberExpression.Body).Member;
        }
    }
}
