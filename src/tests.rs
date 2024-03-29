use crate::{FunctionParameter, LibState, ReturnValue};

#[test]
fn dotnet_add() {
    let mut libstate = LibState::create();
    let func_id = libstate
        .create_static_dotnet_function_id("SpisharpCS.Tests.AddTest, SpisharpCS", "Add")
        .expect("should have created a static dotnet function id");
    
    let result = libstate
        .call_static_dotnet_func(
            func_id,
            &[FunctionParameter::Int(3), FunctionParameter::Int(2)],
        )
        .expect("should have called add in dotnet");
    assert_eq!(result, ReturnValue::Int(5));

    let result = libstate.call_static_dotnet_func(func_id, &[FunctionParameter::Int(360), FunctionParameter::Int(17)]).expect("should have called dotnet add function");
    assert_eq!(result, ReturnValue::Int(377));
}
