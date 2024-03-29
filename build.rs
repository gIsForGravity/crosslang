use std::env;
use std::fs;
use std::path::PathBuf;
use std::process::Command;

const DOTNET_SOLUTION_DIR: &str = "crosslangnet";
const DOTNET_PROJECT_FILE: &str = "crosslangnet/crosslangnet.csproj";
const DOTNET_BUILD_DIR: &str = "crosslangnet/crosslangnet/bin/Debug/net8.0";

fn main() {
    // Setting build script options:
    println!("cargo:rerun-if-changed=crosslangnet/crosslangnet");

    // running dotnet build
    let mut command = Command::new("dotnet")
        .arg("build")
        .arg(DOTNET_PROJECT_FILE)
        .current_dir(DOTNET_SOLUTION_DIR)
        .spawn()
        .expect("should have run dotnet build");

    command.wait().expect("didn't wait");

    copy("crosslangnet.dll");
    // copy("crosslangnet.deps.json");
    copy("crosslangnet.pdb");
    copy("crosslangnet.runtimeconfig.json");
}

fn copy(name: &str) {
    let old_file_path = PathBuf::from(DOTNET_BUILD_DIR).as_path().join(name);
    let new_file_path = env::current_dir().unwrap().as_path().join(name);
    fs::copy(old_file_path, new_file_path).expect("copying failed");
}
