// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Protocols.TestTools.Messages.Marshaling
{
    internal class ValueExtractor : IExpressionVisitor
    {
        private string expr;

        public string Expression
        {
            get { return expr; }
            set { expr = value; }
        }

        public void Visit(IExpression expression)
        {
            throw new NotImplementedException();
        }

        public void Visit(UnaryExpression expression)
        {
            throw new NotImplementedException();
        }

        public void Visit(BinaryExpression expression)
        {
            throw new NotImplementedException();
        }

        public void Visit(ConditionalExpression expression)
        {
            throw new NotImplementedException();
        }

        public void Visit(ValueExpression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            expr = expression.Text;
        }

        public void Visit(FunctionExpression expression)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Expression Visitor
    /// </summary>
    public class ExpressionVisitor : IExpressionVisitor
    {
        private object result;
        private IEvaluationContext context;
        
        /// <summary>
        /// Evaluation result
        /// </summary>
        public object EvaluationResult
        {
            get
            {
                return result;
            }
        }

        /// <summary>
        /// Expression visitor
        /// </summary>
        /// <param name="context">Evaluation context</param>
        public ExpressionVisitor(IEvaluationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            this.context = context;
        }

        /// <summary>
        /// Visits an expression (method not implemented)
        /// </summary>
        /// <param name="expression">The expression</param>
        /// <exception cref="NotImplementedException">Thrown if this method is called</exception>
        public void Visit(IExpression expression)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a binary expression
        /// </summary>
        /// <param name="expression">The binary expression</param>
        /// <exception cref="ArgumentNullException">Thrown when the input parameter is null</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public void Visit(BinaryExpression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            expression.LeftExpression.Accept(this);
            object left = result;

            expression.RightExpression.Accept(this);
            object right = result;

            if (left is long && right is long)
            {
                switch (expression.Type)
                {
                    case BinaryExpressionType.Plus:
                        result = Convert.ToInt64(left) + Convert.ToInt64(right);
                        break;
                    case BinaryExpressionType.Minus:
                        result = Convert.ToInt64(left) - Convert.ToInt64(right);
                        break;
                    case BinaryExpressionType.Multiply:
                        result = Convert.ToInt64(left) * Convert.ToInt64(right);
                        break;
                    case BinaryExpressionType.Div:
                        result = Convert.ToInt64(left) / Convert.ToInt64(right);
                        break;
                    case BinaryExpressionType.Mod:
                        result = Convert.ToInt64(left) % Convert.ToInt64(right);
                        break;
                    case BinaryExpressionType.ShiftLeft:
                        result = Convert.ToInt64(left) << Convert.ToInt32(right);
                        break;
                    case BinaryExpressionType.ShiftRight:
                        result = Convert.ToInt64(left) >> Convert.ToInt32(right);
                        break;
                    case BinaryExpressionType.GreaterOrEqual:
                        result = (Convert.ToInt64(left) >= Convert.ToInt64(right)) ? 1 : 0;
                        break;
                    case BinaryExpressionType.LesserOrEqual:
                        result = (Convert.ToInt64(left) <= Convert.ToInt64(right)) ? 1 : 0;
                        break;
                    case BinaryExpressionType.Greater:
                        result = (Convert.ToInt64(left) > Convert.ToInt64(right)) ? 1 : 0;
                        break;
                    case BinaryExpressionType.Lesser:
                        result = (Convert.ToInt64(left) < Convert.ToInt64(right)) ? 1 : 0;
                        break;
                    case BinaryExpressionType.Equal:
                        result = (Convert.ToInt64(left) == Convert.ToInt64(right)) ? 1 : 0;
                        break;
                    case BinaryExpressionType.NotEqual:
                        result = (Convert.ToInt64(left) != Convert.ToInt64(right)) ? 1 : 0;
                        break;
                    case BinaryExpressionType.BitXor:
                        result = Convert.ToInt64(left) ^ Convert.ToInt64(right);
                        break;
                    case BinaryExpressionType.BitAnd:
                        result = Convert.ToInt64(left) & Convert.ToInt64(right);
                        break;
                    case BinaryExpressionType.BitOr:
                        result = Convert.ToInt64(left) | Convert.ToInt64(right);
                        break;
                    case BinaryExpressionType.And:
                        bool andResult = Convert.ToBoolean(left) && Convert.ToBoolean(right);
                        result = Convert.ToInt64(andResult);
                        break;
                    case BinaryExpressionType.Or:
                        bool orResult =  Convert.ToBoolean(left) || Convert.ToBoolean(right);
                        result = Convert.ToInt64(orResult);
                        break;
                    default:
                        throw new ExpressionEvaluatorException(
                            String.Format("binary operation '{0}' is not supported.", expression.Type));
                }
            }
            else
            {
                result = String.Format("{0} {1} {2}",
                    left,
                    GetBinaryOperatorString(expression.Type),
                    right);
            }
        }

        /// <summary>
        /// Visits a value expression
        /// </summary>
        /// <param name="expression">The expression</param>
        /// <exception cref="ArgumentNullException">Thrown when the input parameter is null</exception>        
        public void Visit(ValueExpression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            switch (expression.Type)
            {
                case ValueExpressionType.Integer:
                    result = long.Parse(expression.Text);
                    break;
                case ValueExpressionType.Variable:
                    long value;
                    int dereferencedValue;
                    int pointerValue;
                    if (context.TryResolveSymbol(expression.Text, out value))
                    {
                        result = context.Variables[expression.Text];
                    }
                    else if (context.TryResolveDereference(expression.Text, out dereferencedValue, out pointerValue))
                    {
                        // give a dummy pointer address for the pointer.
                        result = pointerValue;
                    }
                    else
                    {
                        int intValue;
                        if (Int32.TryParse(expression.Text, out intValue))
                        {
                            result = intValue;
                        }
                        else
                        {
                            result = expression.Text;
                        }
                    }
                    break;
                case ValueExpressionType.Null:
                    result = expression.Text;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Visits a function expression
        /// </summary>
        /// <param name="expression">The expression</param>
        /// <exception cref="ArgumentNullException">Thrown when the input parameter is null</exception>        
        public void Visit(FunctionExpression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            expression.Expression.Accept(this);
            object param = result;
            if (expression.FunctionName == "sizeof")
            {
                string customTypeName;
                long symbolValue;
                if (param is string)
                {
                    if ((context.TryResolveCustomType((string)param, out customTypeName)
                        && !DatatypeInfoProvider.IsPredefinedDatatype(customTypeName))
                        || param.ToString().StartsWith("enum")
                        || param.ToString().StartsWith("struct")
                        || param.ToString().StartsWith("union"))
                    {
                        result = "sizeof(" + param.ToString() + ")"; 
                    }
                    else
                    {
                        result = DatatypeInfoProvider.GetRpcDatatypeLength((string)param);
                    }
                }
                else if (context.TryResolveSymbol(expression.Text, out symbolValue))
                {
                    Type value = param.GetType();
                    string typeName = String.Empty;
                    switch (value.ToString())
                    {
                        case "System.Char":
                            typeName = "char";
                            break;
                        case "System.Byte":
                            typeName = "byte";
                            break;
                        case "System.Int16":
                            typeName = "short";
                            break;
                        case "System.Int32":
                            typeName = "int";
                            break;
                        case "System.Boolean":
                            typeName = "boolean";
                            break;
                        default:
                            break;
                    }

                    result = DatatypeInfoProvider.GetRpcDatatypeLength(typeName);                    
                }
                if (result is long
                    && (long)result <= 0)
                {
                    throw new ExpressionEvaluatorException(
                        String.Format("cannot get the datatype length for the datatype '{0}'", param));
                }
            }
        }

        /// <summary>
        /// Visits a unary expression
        /// </summary>
        /// <param name="expression">The expression</param>
        /// <exception cref="ArgumentNullException">Thrown when the input parameter is null</exception>        
        public void Visit(UnaryExpression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            expression.Expression.Accept(this);
            if (result is long)
            {
                long param = (long)result;
                switch (expression.Type)
                {
                    case UnaryExpressionType.Not:
                        result = param != 0 ? 0 : 1;
                        break;
                    case UnaryExpressionType.BitNot:
                        result = ~param;
                        break;
                    case UnaryExpressionType.Positive:
                        result = +param;
                        break;
                    case UnaryExpressionType.Negative:
                        result = -param;
                        break;
                    case UnaryExpressionType.Dereference:
                        int value;
                        int pointerValue;
                        if (param == 1 || param == 0)
                        {
                            ValueExtractor ve = new ValueExtractor();
                            expression.Expression.Accept(ve);
                            string expr = ve.Expression;
                            if (context.TryResolveDereference(expr, out value, out pointerValue))
                            {
                                result = value;
                            }
                        }
                        else
                        {
                            IntPtr p = new IntPtr(param);
                            result = System.Runtime.InteropServices.Marshal.ReadInt32(p);
                        }
                        break;
                    default:
                        break;
                }
            }
            else if (result is string)
            {
                result = GetUnaryOperatorString(expression.Type) + result.ToString();
            }
        }

        /// <summary>
        /// Visits a conditional expression
        /// </summary>
        /// <param name="expression">The expression</param>
        /// <exception cref="ArgumentNullException">Thrown when the input parameter is null</exception>        
        public void Visit(ConditionalExpression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            expression.FirstExpression.Accept(this);
            object first = result;

            if (first is long)
            {
                if (Convert.ToBoolean((long)first))
                {
                    expression.SecondExpression.Accept(this);
                }
                else
                {
                    expression.ThirdExpression.Accept(this);
                }
            }
            else
            {
                expression.SecondExpression.Accept(this);
                object second = result;

                expression.ThirdExpression.Accept(this);
                object third = result;
                result = String.Format("{0} ? {1} : {2}", first, second, third);
            }
        }

        /// <summary>
        /// Gets binary operator string by expression type
        /// </summary>
        /// <param name="type">The binary expression type</param>
        /// <returns>The binary operator string</returns>
        protected static string GetBinaryOperatorString(BinaryExpressionType type)
        {
            switch (type)
            {
                case BinaryExpressionType.Plus:
                    return "+";
                case BinaryExpressionType.Minus:
                    return "-";
                case BinaryExpressionType.Multiply:
                    return "*";
                case BinaryExpressionType.Div:
                    return "/";
                case BinaryExpressionType.Mod:
                    return "%";
                case BinaryExpressionType.BitAnd:
                    return "&";
                default:
                    throw new ExpressionEvaluatorException("unknown binary operator");
            }
        }

        /// <summary>
        /// Gets unary operator string by unary expression type
        /// </summary>
        /// <param name="type">The unary expression type</param>
        /// <returns>The unary operator string</returns>
        protected static string GetUnaryOperatorString(UnaryExpressionType type)
        {
            switch (type)
            {
                case UnaryExpressionType.Not:
                    return "!";
                case UnaryExpressionType.BitNot:
                    return "~";
                case UnaryExpressionType.Negative:
                    return "-";
                case UnaryExpressionType.Positive:
                    return "+";
                case UnaryExpressionType.Dereference:
                    return "*";
                default:
                    throw new ExpressionEvaluatorException("unknown unary operator");
            }
        }
    }
}
