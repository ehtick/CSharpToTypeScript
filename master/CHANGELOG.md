# Change log
All notable changes to CSharpToTypeScript will be documented in this file.

## 1.0.0.5 (2023-05-09)

### Changed

Generate `valueAsNumber = true` for enum types in React + Bootstrap forms because C# API server cannot deserialize the values as strings. Sending a '1' would cause JSON deserialization failure, for example. With `valueAsNumber = true`, a value of 1 will be sent instead:
```
<div className="form-group col-md-4">
	<label htmlFor={formId + "-status"}>Status:</label>
	<select className={getClassName(touchedFields.status, errors.status)} id={formId + "-status"} {...register("status", { valueAsNumber: true })}>
		<option value="">Select a Status</option>
		<option value="0">Unknown</option>
		<option value="1">Registered</option>
		<option value="2">Unregistered</option>
		<option value="3">Approval Pending</option>
	</select>
	{getErrorMessage(errors.status)}
</div>
```
