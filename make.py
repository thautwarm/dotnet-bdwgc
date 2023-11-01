#!/usr/bin/env python
from pmakefile import *  # type: ignore
import subprocess

phony(["all", "build-bflat", "run", "run-bflat"])

ROOT = Path(__file__).parent


def gen_cache(f: Path):
    x = f.stat().st_mtime
    return str(x)


def load_cache(cache_f: Path):
    try:
        return cache_f.read_text()
    except FileNotFoundError:
        return ""


@recipe()
def build_bflat():
    Path("build").mkdir(exist_ok=True, parents=True)
    os.chdir("build")
    if gen_cache(Path("../Program.cs")) != load_cache(Path("Program.cs.cache")):
        code = subprocess.call(["bflat", "build", "../Program.cs", "--stdlib:zero"])
        if code != 0:
            exit(1)

        Path("Program.cs.cache").write_text(gen_cache(Path("../Program.cs")))


@recipe("build-bflat")
def run_bflat():
    PATH = os.environ["PATH"]
    extra = ROOT.joinpath("./deps").absolute().as_posix()
    PATH = os.pathsep.join([extra, *PATH.split(os.pathsep)])

    exe = ROOT.joinpath("./build/Program.exe")
    print(exe)
    code = subprocess.call([exe.as_posix()])
    if code != 0:
        exit(1)


@recipe()
def run():
    PATH = os.environ["PATH"]
    extra = ROOT.joinpath("./deps").absolute().as_posix()
    PATH = os.pathsep.join([extra, *PATH.split(os.pathsep)])

    os.chdir("./deps")
    code = subprocess.call(["dotnet", "run", "--project", "../bdwgc.csproj"])
    if code != 0:
        exit(1)


make()
