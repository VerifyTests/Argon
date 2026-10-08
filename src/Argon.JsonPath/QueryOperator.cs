namespace Argon;

enum QueryOperator
{
    None = 0,
    Equals = 1,
    NotEquals = 2,
    Exists = 3,
    LessThan = 4,
    LessThanOrEquals = 5,
    GreaterThan = 6,
    GreaterThanOrEquals = 7,
    And = 8,
    Or = 9,
    RegexEquals = 10,
    StrictEquals = 11,
    StrictNotEquals = 12
}