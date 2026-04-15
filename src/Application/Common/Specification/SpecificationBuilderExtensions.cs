using Ardalis.Specification;
using Boilerate.Application.Common.Exceptions;
using Boilerate.Application.Common.Models;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace Boilerate.Application.Common.Specification;

public static class SpecificationBuilderExtensions
{
    #region SearchBy & PaginateBy (Entry Points)

    /// <summary>
    /// Extension method để apply tất cả search và filter từ BaseFilter vào specification.
    /// </summary>
    public static ISpecificationBuilder<T> SearchBy<T>(this ISpecificationBuilder<T> query, BaseFilter filter) =>
        query
            .SearchByKeyword(filter.Keyword)
            .AdvancedSearch(filter.AdvancedSearch)
            .AdvancedFilter(filter.AdvancedFilter);

    /// <summary>
    /// Extension method để apply pagination và ordering vào specification.
    /// </summary>
    public static ISpecificationBuilder<T> PaginateBy<T>(this ISpecificationBuilder<T> query, PaginationFilter filter)
    {
        // Validate và set defaults
        if (filter.PageNumber <= 0)
        {
            filter.PageNumber = 1;
        }

        if (filter.PageSize <= 0)
        {
            filter.PageSize = 10;
        }

        // Calculate skip
        if (filter.PageNumber > 1)
        {
            query = query.Skip((filter.PageNumber - 1) * filter.PageSize);
        }

        return query
            .Take(filter.PageSize)
            .OrderBy(filter.OrderBy);
    }

    #endregion

    #region Search Methods

    /// <summary>
    /// Extension method để search đơn giản với keyword trong tất cả fields.
    /// </summary>
    public static IOrderedSpecificationBuilder<T> SearchByKeyword<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        string? keyword) =>
        specificationBuilder.AdvancedSearch(new Search { Keyword = keyword });

    /// <summary>
    /// Extension method để search nâng cao với keyword trong các fields cụ thể hoặc tất cả fields.
    /// </summary>
    public static IOrderedSpecificationBuilder<T> AdvancedSearch<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        Search? search)
    {
        if (!string.IsNullOrEmpty(search?.Keyword))
        {
            if (search.Fields?.Length > 0)
            {
                // Search trong các fields được chỉ định
                foreach (string field in search.Fields)
                {
                    var paramExpr = Expression.Parameter(typeof(T));
                    MemberExpression propertyExpr = GetPropertyExpression(field, paramExpr);
                    specificationBuilder.AddSearchPropertyByKeyword(propertyExpr, paramExpr, search.Keyword);
                }
            }
            else
            {
                // Search trong TẤT CẢ primitive fields
                foreach (var property in typeof(T).GetProperties()
                    .Where(prop =>
                        (Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType) is { } propertyType
                        && !propertyType.IsEnum
                        && Type.GetTypeCode(propertyType) != TypeCode.Object))
                {
                    var paramExpr = Expression.Parameter(typeof(T));
                    var propertyExpr = Expression.Property(paramExpr, property);
                    specificationBuilder.AddSearchPropertyByKeyword(propertyExpr, paramExpr, search.Keyword);
                }
            }
        }

        return new OrderedSpecificationBuilder<T>(specificationBuilder.Specification);
    }

    /// <summary>
    /// Private helper method để thêm search criteria cho một property cụ thể.
    /// </summary>
    private static void AddSearchPropertyByKeyword<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        Expression propertyExpr,
        ParameterExpression paramExpr,
        string keyword,
        string operatorSearch = FilterOperator.CONTAINS)
    {
        if (propertyExpr is not MemberExpression memberExpr || memberExpr.Member is not PropertyInfo property)
        {
            throw new ArgumentException("propertyExpr must be a property expression.", nameof(propertyExpr));
        }

        // Tạo search pattern
        string searchTerm = operatorSearch switch
        {
            FilterOperator.STARTSWITH => $"{keyword.ToLower()}%",
            FilterOperator.ENDSWITH => $"%{keyword.ToLower()}",
            FilterOperator.CONTAINS => $"%{keyword.ToLower()}%",
            _ => throw new ArgumentException("operatorSearch is not valid.", nameof(operatorSearch))
        };

        // Build selector expression
        Expression selectorExpr =
            property.PropertyType == typeof(string)
                ? propertyExpr
                : Expression.Condition(
                    Expression.Equal(
                    Expression.Convert(propertyExpr, typeof(object)),
                    Expression.Constant(null, typeof(object))),
                    Expression.Constant(null, typeof(string)),
                    Expression.Call(propertyExpr, "ToString", null, null));

        // Convert to lowercase
        var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
        Expression callToLowerMethod = Expression.Call(selectorExpr, toLowerMethod!);
        var selector = Expression.Lambda<Func<T, string>>(callToLowerMethod, paramExpr);

        // Add to SearchCriterias
        ((List<SearchExpressionInfo<T>>)specificationBuilder.Specification.SearchCriterias)
            .Add(new SearchExpressionInfo<T>(selector, searchTerm, 1));
    }

    #endregion

    #region Filter Methods

    /// <summary>
    /// Extension method để apply advanced filter với operators và logic.
    /// </summary>
    public static IOrderedSpecificationBuilder<T> AdvancedFilter<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        Filter? filter)
    {
        if (filter is not null)
        {
            var parameter = Expression.Parameter(typeof(T));
            Expression binaryExpresioFilter;

            if (!string.IsNullOrEmpty(filter.Logic))
            {
                if (filter.Filters is null)
                {
                    throw new CustomException("The Filters attribute is required when declaring a logic");
                }

                binaryExpresioFilter = CreateFilterExpression(filter.Logic, filter.Filters, parameter);
            }
            else
            {
                var filterValid = GetValidFilter(filter);
                binaryExpresioFilter = CreateFilterExpression(
                    filterValid.Field!,
                    filterValid.Operator!,
                    filterValid.Value,
                    parameter);
            }

            ((List<WhereExpressionInfo<T>>)specificationBuilder.Specification.WhereExpressions)
                .Add(new WhereExpressionInfo<T>(
                    Expression.Lambda<Func<T, bool>>(binaryExpresioFilter, parameter)));
        }

        return new OrderedSpecificationBuilder<T>(specificationBuilder.Specification);
    }

    /// <summary>
    /// Build filter expression từ Logic và Filters (recursive).
    /// </summary>
    private static Expression CreateFilterExpression(
        string logic,
        IEnumerable<Filter> filters,
        ParameterExpression parameter)
    {
        Expression filterExpression = default!;

        foreach (var filter in filters)
        {
            Expression bExpresionFilter;

            if (!string.IsNullOrEmpty(filter.Logic))
            {
                if (filter.Filters is null)
                {
                    throw new CustomException("The Filters attribute is required when declaring a logic");
                }

                bExpresionFilter = CreateFilterExpression(filter.Logic, filter.Filters, parameter);
            }
            else
            {
                var filterValid = GetValidFilter(filter);
                bExpresionFilter = CreateFilterExpression(
                    filterValid.Field!,
                    filterValid.Operator!,
                    filterValid.Value,
                    parameter);
            }

            filterExpression = filterExpression is null
                ? bExpresionFilter
                : CombineFilter(logic, filterExpression, bExpresionFilter);
        }

        return filterExpression;
    }

    /// <summary>
    /// Build filter expression từ Field, Operator, Value.
    /// </summary>
    private static Expression CreateFilterExpression(
        string field,
        string filterOperator,
        object? value,
        ParameterExpression parameter)
    {
        var propertyExpresion = GetPropertyExpression(field, parameter);
        var valueExpresion = GeValuetExpression(field, value, propertyExpresion.Type);
        return CreateFilterExpression(propertyExpresion, valueExpresion, filterOperator);
    }

    /// <summary>
    /// Build binary expression từ member expression và constant expression.
    /// </summary>
    private static Expression CreateFilterExpression(
        Expression memberExpression,
        Expression constantExpression,
        string filterOperator)
    {
        // Case-insensitive string comparison
        if (memberExpression.Type == typeof(string))
        {
            constantExpression = Expression.Call(constantExpression, "ToLower", null);
            memberExpression = Expression.Call(memberExpression, "ToLower", null);
        }

        return filterOperator switch
        {
            FilterOperator.EQ => Expression.Equal(memberExpression, constantExpression),
            FilterOperator.NEQ => Expression.NotEqual(memberExpression, constantExpression),
            FilterOperator.LT => Expression.LessThan(memberExpression, constantExpression),
            FilterOperator.LTE => Expression.LessThanOrEqual(memberExpression, constantExpression),
            FilterOperator.GT => Expression.GreaterThan(memberExpression, constantExpression),
            FilterOperator.GTE => Expression.GreaterThanOrEqual(memberExpression, constantExpression),
            FilterOperator.CONTAINS => Expression.Call(memberExpression, "Contains", null, constantExpression),
            FilterOperator.STARTSWITH => Expression.Call(memberExpression, "StartsWith", null, constantExpression),
            FilterOperator.ENDSWITH => Expression.Call(memberExpression, "EndsWith", null, constantExpression),
            _ => throw new CustomException("Filter Operator is not valid."),
        };
    }

    /// <summary>
    /// Kết hợp hai expressions với logic operator.
    /// </summary>
    private static Expression CombineFilter(
        string filterOperator,
        Expression bExpresionBase,
        Expression bExpresion) => filterOperator switch
        {
            FilterLogic.AND => Expression.And(bExpresionBase, bExpresion),
            FilterLogic.OR => Expression.Or(bExpresionBase, bExpresion),
            FilterLogic.XOR => Expression.ExclusiveOr(bExpresionBase, bExpresion),
            _ => throw new ArgumentException("FilterLogic is not valid."),
        };

    #endregion

    #region OrderBy Methods

    /// <summary>
    /// Extension method để apply ordering vào specification.
    /// </summary>
    public static IOrderedSpecificationBuilder<T> OrderBy<T>(
        this ISpecificationBuilder<T> specificationBuilder,
        string[]? orderByFields)
    {
        if (orderByFields is not null)
        {
            foreach (var field in ParseOrderBy(orderByFields))
            {
                var paramExpr = Expression.Parameter(typeof(T));

                Expression propertyExpr = paramExpr;
                foreach (string member in field.Key.Split('.'))
                {
                    propertyExpr = Expression.PropertyOrField(propertyExpr, member);
                }

                var keySelector = Expression.Lambda<Func<T, object?>>(
                    Expression.Convert(propertyExpr, typeof(object)),
                    paramExpr);

                ((List<OrderExpressionInfo<T>>)specificationBuilder.Specification.OrderExpressions)
                    .Add(new OrderExpressionInfo<T>(keySelector, field.Value));
            }
        }

        return new OrderedSpecificationBuilder<T>(specificationBuilder.Specification);
    }

    /// <summary>
    /// Parse orderByFields array thành Dictionary.
    /// </summary>
    private static Dictionary<string, OrderTypeEnum> ParseOrderBy(string[] orderByFields) =>
        new(orderByFields.Select((orderByfield, index) =>
        {
            string[] fieldParts = orderByfield.Split(' ');
            string field = fieldParts[0];
            bool descending = fieldParts.Length > 1 &&
                fieldParts[1].StartsWith("Desc", StringComparison.OrdinalIgnoreCase);

            var orderBy = index == 0
                ? descending ? OrderTypeEnum.OrderByDescending : OrderTypeEnum.OrderBy
                : descending ? OrderTypeEnum.ThenByDescending : OrderTypeEnum.ThenBy;

            return new KeyValuePair<string, OrderTypeEnum>(field, orderBy);
        }));

    #endregion

    #region Helper Methods

    /// <summary>
    /// Build property expression từ property name (support nested properties).
    /// </summary>
    private static MemberExpression GetPropertyExpression(
        string propertyName,
        ParameterExpression parameter)
    {
        Expression propertyExpression = parameter;

        foreach (string member in propertyName.Split('.'))
        {
            propertyExpression = Expression.PropertyOrField(propertyExpression, member);
        }

        return (MemberExpression)propertyExpression;
    }

    /// <summary>
    /// Extract string từ JsonElement.
    /// </summary>
    private static string GetStringFromJsonElement(object value)
        => ((JsonElement)value).GetString()!;

    /// <summary>
    /// Convert value thành ConstantExpression với type phù hợp.
    /// </summary>
    private static ConstantExpression GeValuetExpression(
        string field,
        object? value,
        Type propertyType)
    {
        if (value == null)
        {
            return Expression.Constant(null, propertyType);
        }

        // Handle Enum
        if (propertyType.IsEnum)
        {
            string? stringEnum = GetStringFromJsonElement(value);
            if (!Enum.TryParse(propertyType, stringEnum, true, out object? valueparsed))
            {
                throw new CustomException($"Value {value} is not valid for {field}");
            }

            return Expression.Constant(valueparsed, propertyType);
        }

        // Handle Guid
        if (propertyType == typeof(Guid))
        {
            string? stringGuid = GetStringFromJsonElement(value);
            if (!Guid.TryParse(stringGuid, out Guid valueparsed))
            {
                throw new CustomException($"Value {value} is not valid for {field}");
            }

            return Expression.Constant(valueparsed, propertyType);
        }

        // Handle String
        if (propertyType == typeof(string))
        {
            string? text = GetStringFromJsonElement(value);
            return Expression.Constant(text, propertyType);
        }

        // Handle DateTime
        if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
        {
            string? text = GetStringFromJsonElement(value);
            return Expression.Constant(ChangeType(text, propertyType), propertyType);
        }

        // Handle other types
        return Expression.Constant(
            ChangeType(((JsonElement)value).GetRawText(), propertyType),
            propertyType);
    }

    /// <summary>
    /// Convert value sang type khác (handle Nullable types).
    /// </summary>
    public static dynamic? ChangeType(object value, Type conversion)
    {
        var t = conversion;

        if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
        {
            if (value == null)
            {
                return null;
            }

            t = Nullable.GetUnderlyingType(t);
        }

        return Convert.ChangeType(value, t!);
    }

    /// <summary>
    /// Validate Filter object.
    /// </summary>
    private static Filter GetValidFilter(Filter filter)
    {
        if (string.IsNullOrEmpty(filter.Field))
        {
            throw new CustomException("The field attribute is required when declaring a filter");
        }

        if (string.IsNullOrEmpty(filter.Operator))
        {
            throw new CustomException("The Operator attribute is required when declaring a filter");
        }

        return filter;
    }

    #endregion
}
