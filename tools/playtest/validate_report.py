"""Checks a playtest report.json against report.schema.json.

    python3 -I tools/playtest/validate_report.py tools/playtest/report.schema.json SESSION/report.json

Standard library only, so it implements just the JSON Schema keywords the
schema uses: type, enum, required, properties, additionalProperties (true,
false or a schema), items, minimum, maximum, minLength and maxLength. Prints
"ok" or one line per problem; exits 0 when the report is valid.
"""

import json
import os
import sys

TYPES = {
    "object": lambda v: isinstance(v, dict),
    "array": lambda v: isinstance(v, list),
    "string": lambda v: isinstance(v, str),
    "integer": lambda v: isinstance(v, int) and not isinstance(v, bool),
    "number": lambda v: isinstance(v, (int, float)) and not isinstance(v, bool),
    "boolean": lambda v: isinstance(v, bool),
    "null": lambda v: v is None,
}


def validate(value, schema, path="$"):
    """Every way value fails schema, as "path: problem" strings."""
    errors = []
    expected = schema.get("type")
    if expected is not None:
        names = expected if isinstance(expected, list) else [expected]
        if not any(TYPES[name](value) for name in names):
            return ["%s: expected %s, got %s" % (path, " or ".join(names), type(value).__name__)]
    if "enum" in schema and value not in schema["enum"]:
        errors.append("%s: %r is not one of %s" % (path, value, ", ".join(map(str, schema["enum"]))))
    if isinstance(value, str):
        if "minLength" in schema and len(value) < schema["minLength"]:
            errors.append("%s: shorter than %d characters" % (path, schema["minLength"]))
        if "maxLength" in schema and len(value) > schema["maxLength"]:
            errors.append("%s: longer than %d characters" % (path, schema["maxLength"]))
    if TYPES["number"](value):
        if "minimum" in schema and value < schema["minimum"]:
            errors.append("%s: %s is below %s" % (path, value, schema["minimum"]))
        if "maximum" in schema and value > schema["maximum"]:
            errors.append("%s: %s is above %s" % (path, value, schema["maximum"]))
    if isinstance(value, dict):
        for key in schema.get("required", []):
            if key not in value:
                errors.append("%s: missing required '%s'" % (path, key))
        properties = schema.get("properties", {})
        extra = schema.get("additionalProperties", True)
        for key, item in value.items():
            where = "%s.%s" % (path, key)
            if key in properties:
                errors.extend(validate(item, properties[key], where))
            elif extra is False:
                errors.append("%s: unexpected property" % where)
            elif isinstance(extra, dict):
                errors.extend(validate(item, extra, where))
    if isinstance(value, list) and "items" in schema:
        for i, item in enumerate(value):
            errors.extend(validate(item, schema["items"], "%s[%d]" % (path, i)))
    return errors


def check_report(report, schema, report_dir=None):
    """Schema problems, plus screenshots that do not exist (relative paths are taken from report_dir)."""
    errors = validate(report, schema)
    if not errors and report_dir is not None:
        for i, finding in enumerate(report.get("findings", [])):
            shot = finding.get("screenshot")
            if shot and not os.path.isfile(shot if os.path.isabs(shot) else os.path.join(report_dir, shot)):
                errors.append("$.findings[%d].screenshot: no such file %s" % (i, shot))
    return errors


def main(argv):
    if len(argv) != 3:
        print(__doc__.strip())
        return 2
    schema_path, report_path = argv[1], argv[2]
    with open(schema_path, encoding="utf-8") as f:
        schema = json.load(f)
    try:
        with open(report_path, encoding="utf-8") as f:
            report = json.load(f)
    except OSError as e:
        print("%s: cannot read: %s" % (report_path, e.strerror))
        return 1
    except ValueError as e:
        print("%s: not valid JSON: %s" % (report_path, e))
        return 1
    errors = check_report(report, schema, os.path.dirname(os.path.abspath(report_path)))
    for error in errors:
        print(error)
    if not errors:
        print("ok: %s is a valid report (%d findings)" % (report_path, len(report["findings"])))
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
