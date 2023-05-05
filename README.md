# CSharpToTypeScript
Generate Typescript code and React-Hook-Form Resolver code from a C# class definition. This project started from TypeLITE 1.8.2.0 source code, with the assumption that the license statement at http://type.litesolutions.net/license grants permission to develop open source code project such as this one.

Start with the C# class

```
using System.ComponentModel.DataAnnotations;

namespace CSharpToTypeScript.Test
{
    public class PersonWithValidation
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Range(20, 120)]
        public Int32? Age { get; set; }

        [StringLength(120, MinimumLength = 2)]
        public string? Location { get; set; }
    }
}
```

this tool can generate the following TypeScript:
```
import { Resolver, FieldErrors } from 'react-hook-form';

export class PersonWithValidation {
	age?: number | null;
	id?: number;
	location?: string | null;
	name: string;

	constructor(name: string) {
		this.name = name;
	}
}

export const PersonWithValidationResolver: Resolver<PersonWithValidation> = async (values) => {
	const errors: FieldErrors<PersonWithValidation> = {};

	if (values.age) {
		if (values.age > 120) {
			errors.age = {
				type: 'max',
				message: 'Age cannot exceed 120.'
			};
		}
		if (values.age < 20) {
			errors.age = {
				type: 'min',
				message: 'Age cannot be less than 20.'
			};
		}
	}
	if ((values.location?.length ?? 0) > 120) {
		errors.location = {
			type: 'maxLength',
			message: 'Location cannot exceed 120 characters.'
		};
	}
	if ((values.location?.length ?? 0) < 2) {
		errors.location = {
			type: 'minLength',
			message: 'Location cannot be less than 2 characters long.'
		};
	}
	if (!values.name) {
		errors.name = {
			type: 'required',
			message: 'Name is required.'
		};
	}
	if ((values.name?.length ?? 0) > 50) {
		errors.name = {
			type: 'maxLength',
			message: 'Name cannot exceed 50 characters.'
		};
	}

	return {
		values,
		errors
	};
};

```

This tool can be launched from the command line, or from Visual Studio via its ppopup menu in C# source files:

![alt text](https://github.com/tan00001/CSharpToTypeScript/blob/main/VSMenuScreenShot.png)

I would like to express my gratitude to Mr. Lukas Kabrt for his inspiring TypeLITE project, which is the basis of this open source project. I greatly appreciate his dedication to creating valuable open source tools and encourage everyone to explore TypeLITE at http://type.litesolutions.net/.

CSharpToTypeScript is under the <a href="https://opensource.org/license/mit/">MIT License</a>.