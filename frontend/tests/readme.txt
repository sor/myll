
Beware: New *.myll-files must set to be "Content" and "Copy if newer"

The virtual Expr.Gen should be protected and Expr itself should
	provide a Gen that also takes into account the level of indentation.

Test EnumerateDF if it really works (see shelf)

For proper Analyze:
	Program.CompileModule needs to be split up,
	1.	The Visit needs to happen for each module [para]
	2.	All these modules need to get to know each other [seq]
	3.	Each module can properly determine the types [para]
	4.	If there is still something undetermined loop to 2.
			or the type or name could be external (warn)


