import "./index.scss"

import Lumia800 from "~/static/lumia-800.jpg"
import { useTitle } from '~/util/effects';

const Index = () => {
    useTitle("home");

    return (
        <>
            <div class="hero">
                <img class="hero-img" src={Lumia800} />
                <div class="hero-text-container">
                    <div class="hero-text">
                        <h1>ReLive for Windows Phone</h1>
                        {/* <h2>your windows phone, restored</h2> */}
                        <h2 class="text-accent">coming soon</h2>
                    </div>
                </div>
            </div>

            <div class="features">
                <div class="feature">
                    <h3 class="background-accent">connected</h3>
                    <p>Let some fresh air through your Windows Phone. ReLive restores all the Windows Live functionality you fell in love with from Windows Phone 7.</p>
                    <p>Use your phone like it's 2012 all over again.</p>
                    <p><a href="/features">Learn more about features</a></p>
                </div>
                <div class="feature">
                    <h3 class="background-accent">consistent</h3>
                    <p>Built on an open-source backend, with more features and improvements coming all the time.</p>
                    <p>Your Windows Phone will never sit forgotten again.</p>
                    <p><a href="/features/open-source">Learn more about contributing</a></p>
                </div>
                <div class="feature">
                    <h3 class="background-accent">compatible</h3>
                    <p>Escape the walled garden! Explore alternative services never-before-seen on the Windows Phone platfom.</p>
                    <p>Access Bluesky (etc) like it was there from the very start.</p>
                    <p><a href="/features/services">Learn more about services</a></p>
                </div>
            </div>
        </>
    )
};

export default Index;