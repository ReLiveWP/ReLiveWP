import { useMemo } from "preact/hooks";
import WP70 from "~/static/os/wp70.png"
import WP75 from "~/static/os/wp75.png"
import WP78 from "~/static/os/wp78.png"

type Version = [major: number, minor: number, build: number, revision: number];

const VERSION_MIN_7_8: Version = [7, 10, 8800, 0];
const VERSION_MIN_7_5: Version = [7, 10, 7510, 0];
const VERSION_MIN_7_0_RTM: Version = [7, 0, 7000, 0];
const VERSION_MIN_7_0_SERIES: Version = [7, 0, 6077, 0];

const versionGreaterThan = (
    [major_a, minor_a, build_a, revision_a]: Version,
    [major_b, minor_b, build_b, revision_b]: Version) => {
    return (
        major_a !== major_b ? major_a > major_b :
            minor_a !== minor_b ? minor_a > minor_b :
                build_a !== build_b ? build_a > build_b :
                    revision_a > revision_b
    );
}

export default function useVersion(os_version: string) {
    return useMemo(() => {
        let version = os_version.split('.', 4).map(v => parseInt(v)) as Version;
        if (versionGreaterThan(version, VERSION_MIN_7_8)) {
            return ["7.8", true, WP78]
        }

        if (versionGreaterThan(version, VERSION_MIN_7_5)) {
            return ["7.5", false, WP75]
        }

        if (versionGreaterThan(version, VERSION_MIN_7_0_RTM)) {
            return ["7", false, WP70]
        }

        if (versionGreaterThan(version, VERSION_MIN_7_0_SERIES)) {
            return ["7 Series", false, WP70]
        }

        return ["Unknown", true, null];
    }, [os_version])
}